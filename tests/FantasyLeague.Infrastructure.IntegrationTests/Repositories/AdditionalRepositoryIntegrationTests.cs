using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Domain.Entities.Auth;
using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.IntegrationTests.Database;
using FantasyLeague.Infrastructure.Repositories.Drafts;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.Leagues;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using FantasyLeague.Infrastructure.Repositories.Users;

namespace FantasyLeague.Infrastructure.IntegrationTests.Repositories;

[Collection(PostgreSqlCollection.Name)]
public sealed class AdditionalRepositoryIntegrationTests(PostgreSqlFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserRepository_GetPagedAsync_ReturnsRequestedPageAndTotal()
    {
        await using var context = database.CreateContext();
        context.AddRange(IntegrationTestData.CreateUser("charlie"),
            IntegrationTestData.CreateUser("alpha"), IntegrationTestData.CreateUser("bravo"));
        await context.SaveChangesAsync();

        var result = await new UserRepository(context).GetPagedAsync(
            new PaginationRequest { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("charlie", result.Items.Single().Username);
    }

    [Fact]
    public async Task UserRepository_ExistsAsync_MatchesUsernameCaseInsensitively()
    {
        await using var context = database.CreateContext();
        context.Add(IntegrationTestData.CreateUser("CaseUser"));
        await context.SaveChangesAsync();

        Assert.True(await new UserRepository(context).ExistsAsync(
            "caseuser", "different@example.com", null, CancellationToken.None));
    }

    [Fact]
    public async Task UserRepository_ExistsAsync_IgnoresExcludedUser()
    {
        await using var context = database.CreateContext();
        var user = IntegrationTestData.CreateUser("excluded");
        context.Add(user);
        await context.SaveChangesAsync();

        Assert.False(await new UserRepository(context).ExistsAsync(
            user.Username, user.Email, user.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UserRepository_GetRefreshTokenAsync_ReturnsPersistedToken()
    {
        await using var context = database.CreateContext();
        var user = IntegrationTestData.CreateUser("token-owner");
        context.AddRange(user, new RefreshToken
        {
            Token = "hashed-refresh-token", JwtId = "jwt-id", UserId = user.Id,
            ExpiryDate = DateTime.UtcNow.AddDays(1), Status = TokenStatus.Active
        });
        await context.SaveChangesAsync();

        var token = await new UserRepository(context).GetRefreshTokenAsync(
            "hashed-refresh-token", CancellationToken.None);

        Assert.Equal(user.Id, token?.UserId);
    }

    [Fact]
    public async Task LeagueRepository_GetPagedAsync_FiltersByStatus()
    {
        await using var context = database.CreateContext();
        await IntegrationTestData.AddLeagueAsync(context, LeagueStatus.Active);

        var result = await new LeagueRepository(context).GetPagedAsync(
            new PaginationRequest(), LeagueStatus.RegistrationOpen, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task LeagueRepository_GetDueForDraftAsync_ExcludesFutureDraft()
    {
        await using var context = database.CreateContext();
        await IntegrationTestData.AddLeagueAsync(
            context, LeagueStatus.RegistrationOpen, DateTime.UtcNow.AddHours(1));

        var due = await new LeagueRepository(context).GetDueForDraftAsync(
            DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(due);
    }

    [Fact]
    public async Task LeagueRepository_GetDraftingAsync_ReturnsOnlyDraftingLeague()
    {
        await using var context = database.CreateContext();
        var (_, _, league) = await IntegrationTestData.AddLeagueAsync(context, LeagueStatus.Drafting);

        var drafting = await new LeagueRepository(context).GetDraftingAsync(CancellationToken.None);

        Assert.Equal(league.Id, Assert.Single(drafting).Id);
    }

    [Fact]
    public async Task FantasyTeamRepository_GetPagedByLeagueIdAsync_OrdersAndPaginates()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("paging-owner");
        context.AddRange(owner2,
            new FantasyTeam { Name = "Zulu", LeagueId = league.Id, OwnerId = owner.Id },
            new FantasyTeam { Name = "Alpha", LeagueId = league.Id, OwnerId = owner2.Id });
        await context.SaveChangesAsync();

        var result = await new FantasyTeamRepository(context).GetPagedByLeagueIdAsync(
            league.Id, new PaginationRequest { PageSize = 1 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Alpha", Assert.Single(result.Items).Name);
    }

    [Fact]
    public async Task FantasyTeamRepository_GetPlayerPoolAsync_ExcludesLeagueRosterPlayers()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = new FantasyTeam { Name = "Pool Team", LeagueId = league.Id, OwnerId = owner.Id };
        var assigned = IntegrationTestData.CreatePlayer(2001, "Assigned");
        var available = IntegrationTestData.CreatePlayer(2002, "Available");
        context.AddRange(team, assigned, available, new FantasyTeamPlayer
        { FantasyTeamId = team.Id, LeagueId = league.Id, NbaPlayerId = assigned.Id });
        await context.SaveChangesAsync();

        var pool = await new FantasyTeamRepository(context).GetPlayerPoolAsync(
            team.Id, new PaginationRequest(), CancellationToken.None);

        Assert.Equal(1, pool.TotalCount);
        Assert.Equal(available.Id, Assert.Single(pool.Items).Id);
    }

    [Fact]
    public async Task NbaPlayerRepository_GetPagedAsync_OrdersByFirstName()
    {
        await using var context = database.CreateContext();
        context.AddRange(IntegrationTestData.CreatePlayer(3001, "Zed"),
            IntegrationTestData.CreatePlayer(3002, "Adam"));
        await context.SaveChangesAsync();

        var result = await new NbaPlayerRepository(context).GetPagedAsync(
            new PaginationRequest { PageSize = 1 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Adam", Assert.Single(result.Items).FirstName);
    }

    [Fact]
    public async Task NbaPlayerRepository_GetByNbaIdsAsync_IgnoresUnknownIds()
    {
        await using var context = database.CreateContext();
        context.Add(IntegrationTestData.CreatePlayer(4001));
        await context.SaveChangesAsync();

        var players = await new NbaPlayerRepository(context).GetByNbaIdsAsync(
            [4001, 9999], CancellationToken.None);

        Assert.Single(players);
        Assert.True(players.ContainsKey(4001));
    }

    [Fact]
    public async Task DraftRepository_IsPlayerUnavailableAsync_DetectsRosterPlayer()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = new FantasyTeam { Name = "Roster Team", LeagueId = league.Id, OwnerId = owner.Id };
        var player = IntegrationTestData.CreatePlayer(5001);
        context.AddRange(team, player, new FantasyTeamPlayer
        { FantasyTeamId = team.Id, LeagueId = league.Id, NbaPlayerId = player.Id });
        await context.SaveChangesAsync();

        Assert.True(await new DraftRepository(context).IsPlayerUnavailableAsync(
            league.Id, player.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DraftRepository_GetPicksAsync_ReturnsOverallPickOrder()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = new FantasyTeam { Name = "Draft Order Team", LeagueId = league.Id, OwnerId = owner.Id };
        context.AddRange(team,
            new DraftPickOrder { LeagueId = league.Id, TeamId = team.Id, Round = 1, PositionInRound = 2, OverallPick = 2 },
            new DraftPickOrder { LeagueId = league.Id, TeamId = team.Id, Round = 1, PositionInRound = 1, OverallPick = 1 });
        await context.SaveChangesAsync();

        var picks = await new DraftRepository(context).GetPicksAsync(league.Id, CancellationToken.None);

        Assert.Equal([1, 2], picks.Select(pick => pick.OverallPick));
    }

    [Fact]
    public async Task LeagueSetupRepository_HasUnfinishedFixturesAsync_RecognizesScheduledGame()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("fixture-owner");
        var home = new FantasyTeam { Name = "Home", LeagueId = league.Id, OwnerId = owner.Id };
        var away = new FantasyTeam { Name = "Away", LeagueId = league.Id, OwnerId = owner2.Id };
        context.AddRange(owner2, home, away, new LeagueFixture
        { LeagueId = league.Id, Week = 1, HomeTeamId = home.Id, AwayTeamId = away.Id, Status = MatchStatus.Scheduled });
        await context.SaveChangesAsync();

        Assert.True(await new LeagueSetupRepository(context).HasUnfinishedFixturesAsync(
            league.Id, CancellationToken.None));
    }
}
