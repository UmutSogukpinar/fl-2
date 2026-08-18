using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.Common.Interfaces.ExternalServices;
using FantasyLeague.Application.Common.Interfaces.Messaging;
using FantasyLeague.Application.Common.Interfaces.Security;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Services.Users;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.IntegrationTests.Database;
using FantasyLeague.Infrastructure.Repositories.Drafts;
using FantasyLeague.Infrastructure.Repositories.Users;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.Leagues;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FantasyLeague.Infrastructure.IntegrationTests.Services;

[Collection(PostgreSqlCollection.Name)]
public sealed class ServiceIntegrationTests(PostgreSqlFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Case: User service reads a persisted user
    // Reasoning: The service and repository should work together over the PostgreSQL database.
    // Expected Result: The requested user response is returned.
    [Fact]
    public async Task UserService_WhenUserExists_ReturnsPersistedUser()
    {
        await using var context = database.CreateContext();
        var user = IntegrationTestData.CreateUser("service-user");
        context.Add(user);
        await context.SaveChangesAsync();
        var service = new UserService(
            new UserRepository(context),
            new TestPasswordHasher());

        var response = await service.GetByIdAsync(user.Id);

        Assert.Equal(user.Email, response.Email);
    }

    // Case: NBA player service pages persisted players
    // Reasoning: Pagination should pass through the service into the real PostgreSQL repository.
    // Expected Result: The persisted player is returned in the paged response.
    [Fact]
    public async Task NbaPlayerService_WhenPlayerExists_ReturnsPagedPlayer()
    {
        await using var context = database.CreateContext();
        var player = IntegrationTestData.CreatePlayer(2001, "Paged");
        context.Add(player);
        await context.SaveChangesAsync();
        var service = new NbaPlayerService(
            new NbaPlayerRepository(context), new TestCacheService());

        var response = await service.GetAsync(new PaginationRequest());

        Assert.Equal(1, response.TotalCount);
        Assert.Equal(player.Id, Assert.Single(response.Items).Id);
    }

    // Case: NBA player sync service persists external players
    // Reasoning: API data should flow through synchronization logic into the real repository.
    // Expected Result: One player and its aggregated season statistics are stored.
    [Fact]
    public async Task NbaPlayerSyncService_WhenApiReturnsPlayer_PersistsPlayerAndStats()
    {
        await using var context = database.CreateContext();
        var service = new NbaPlayerSyncService(
            new TestNbaPlayersApiClient(),
            new NbaPlayerRepository(context),
            new TestCacheService());

        var result = await service.SyncActivePlayersAsync();

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, await context.Set<NbaPlayer>().CountAsync());
        Assert.Equal(1, await context.Set<PlayerStats>().CountAsync());
    }

    // Case: League service reads a persisted league
    // Reasoning: League projections should pass through the service using real repositories.
    // Expected Result: The requested league is returned.
    [Fact]
    public async Task LeagueService_WhenLeagueExists_ReturnsLeague()
    {
        await using var context = database.CreateContext();
        var (_, _, league) = await IntegrationTestData.AddLeagueAsync(context);
        var service = CreateLeagueService(context);

        var response = await service.GetByIdAsync(league.Id);

        Assert.Equal(league.Name, response.Name);
    }

    // Case: Fantasy team service reads a persisted team
    // Reasoning: Team projections should pass through the service using real repositories.
    // Expected Result: The requested fantasy team is returned.
    [Fact]
    public async Task FantasyTeamService_WhenTeamExists_ReturnsTeam()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = new FantasyTeam
        {
            Name = "Service Team",
            LeagueId = league.Id,
            OwnerId = owner.Id
        };
        context.Add(team);
        await context.SaveChangesAsync();
        var service = new FantasyTeamService(
            new FantasyTeamRepository(context),
            new LeagueRepository(context),
            new LeagueSetupRepository(context),
            new UserRepository(context),
            new TestIntegrationEventPublisher());

        var response = await service.GetByIdAsync(team.Id);

        Assert.Equal(team.Name, response.Name);
    }

    // Case: Draft service reads persisted drafting state
    // Reasoning: Draft state should be composed from the league and draft repositories.
    // Expected Result: The drafting league state is returned without losing its status.
    [Fact]
    public async Task DraftService_WhenLeagueIsDrafting_ReturnsDraftState()
    {
        await using var context = database.CreateContext();
        var (_, _, league) = await IntegrationTestData.AddLeagueAsync(
            context, LeagueStatus.Drafting);
        var service = new DraftService(
            new LeagueRepository(context),
            new FantasyTeamRepository(context),
            new LeagueSetupRepository(context),
            new DraftRepository(context));

        var response = await service.GetStateAsync(league.Id);

        Assert.Equal(LeagueStatus.Drafting, response.Status);
        Assert.Empty(response.Picks);
    }

    private static LeagueService CreateLeagueService(
        FantasyLeague.Infrastructure.Context.AppDbContext context) => new(
        new LeagueRepository(context),
        new FantasyTeamRepository(context),
        new LeagueSetupRepository(context),
        new UserRepository(context),
        new NbaPlayerRepository(context),
        NullLogger<LeagueService>.Instance);

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) =>
            passwordHash == Hash(password);
    }

    private sealed class TestIntegrationEventPublisher
        : IIntegrationEventPublisher
    {
        public Task PublishAsync<TMessage>(
            string publisherName,
            TMessage message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class TestCacheService : ICacheService
    {
        public Task<T> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan expiration,
            CancellationToken cancellation = default) => factory(cancellation);

        public void Remove(string key)
        {
        }
    }

    private sealed class TestNbaPlayersApiClient : INbaPlayersApiClient
    {
        public Task<IReadOnlyCollection<ExternalNbaPlayer>> GetActivePlayersAsync(
            int season,
            CancellationToken cancellation) => Task.FromResult<IReadOnlyCollection<ExternalNbaPlayer>>(
            [new(3001, "Sync", "Player", null, "G", 1, 190, 85)]);

        public Task<IReadOnlyCollection<ExternalPlayerGameStats>> GetPlayerStatisticsAsync(
            int season,
            CancellationToken cancellation) => Task.FromResult<IReadOnlyCollection<ExternalPlayerGameStats>>(
            [new(3001, 1, "SYN", "G", 30, 20, 5, 6, 1, 0, 2, 8, 16, 2, 5, 2, 2)]);
    }
}
