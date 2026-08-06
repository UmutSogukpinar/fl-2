using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Entities.Users;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.Database;
using FantasyLeague.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.IntegrationTests.Database;

[Collection(PostgreSqlCollection.Name)]
public sealed class DevelopmentDataSeederIntegrationTests(PostgreSqlFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SeedAsync_CreatesExpectedAggregateCounts()
    {
        await using var context = await SeedAsync();

        Assert.Equal(4, await context.Set<User>().CountAsync());
        Assert.Equal(4, await context.Set<FantasyTeam>().CountAsync());
        Assert.Equal(16, await context.Set<NbaPlayer>().CountAsync());
        Assert.Equal(16, await context.Set<PlayerStats>().CountAsync());
        Assert.Equal(12, await context.Set<FantasyTeamPlayer>().CountAsync());
        Assert.Equal(12, await context.Set<DraftPickOrder>().CountAsync());
        Assert.Equal(6, await context.Set<LeagueFixture>().CountAsync());
    }

    [Fact]
    public async Task SeedAsync_WhenRunTwice_IsIdempotent()
    {
        await using var context = await SeedAsync();
        await DevelopmentDataSeeder.SeedAsync(context, new PasswordHasher());

        Assert.Single(await context.Set<League>().ToArrayAsync());
        Assert.Equal(16, await context.Set<NbaPlayer>().CountAsync());
        Assert.Equal(6, await context.Set<LeagueFixture>().CountAsync());
    }

    [Fact]
    public async Task SeedAsync_HashesEveryUserPassword()
    {
        await using var context = await SeedAsync();
        var users = await context.Set<User>().AsNoTracking().ToArrayAsync();
        var hasher = new PasswordHasher();

        Assert.All(users, user =>
            Assert.True(hasher.Verify(DevelopmentDataSeeder.SeedPassword, user.Password)));
    }

    [Fact]
    public async Task SeedAsync_CreatesOneTeamForEveryUser()
    {
        await using var context = await SeedAsync();
        var owners = await context.Set<FantasyTeam>().Select(team => team.OwnerId).ToArrayAsync();

        Assert.Equal(4, owners.Distinct().Count());
        Assert.Equal(4, await context.Set<User>().CountAsync(user => owners.Contains(user.Id)));
    }

    [Fact]
    public async Task SeedAsync_FillsEveryRosterToConfiguredSize()
    {
        await using var context = await SeedAsync();
        var rosterSize = await context.Set<LeagueSettings>().Select(x => x.RosterSize).SingleAsync();
        var rosterCounts = await context.Set<FantasyTeamPlayer>()
            .GroupBy(player => player.FantasyTeamId)
            .Select(group => group.Count()).ToArrayAsync();

        Assert.Equal(4, rosterCounts.Length);
        Assert.All(rosterCounts, count => Assert.Equal(rosterSize, count));
    }

    [Fact]
    public async Task SeedAsync_AssignsEachRosterPlayerOnlyOnceInLeague()
    {
        await using var context = await SeedAsync();
        var rosterPlayers = await context.Set<FantasyTeamPlayer>()
            .Select(player => player.NbaPlayerId).ToArrayAsync();

        Assert.Equal(rosterPlayers.Length, rosterPlayers.Distinct().Count());
    }

    [Fact]
    public async Task SeedAsync_CreatesCompletedAndScheduledFixtures()
    {
        await using var context = await SeedAsync();

        Assert.Equal(4, await context.Set<LeagueFixture>()
            .CountAsync(fixture => fixture.Status == MatchStatus.Completed));
        Assert.Equal(2, await context.Set<LeagueFixture>()
            .CountAsync(fixture => fixture.Status == MatchStatus.Scheduled));
    }

    [Fact]
    public async Task SeedAsync_CreatesSequentialCompletedDraftOrder()
    {
        await using var context = await SeedAsync();
        var picks = await context.Set<DraftPickOrder>()
            .OrderBy(pick => pick.OverallPick).ToArrayAsync();

        Assert.Equal(Enumerable.Range(1, 12), picks.Select(pick => pick.OverallPick));
        Assert.All(picks, pick => Assert.NotNull(pick.NbaPlayerId));
        Assert.All(picks, pick => Assert.NotNull(pick.PickedAt));
    }

    private async Task<Context.AppDbContext> SeedAsync()
    {
        var context = database.CreateContext();
        await DevelopmentDataSeeder.SeedAsync(context, new PasswordHasher());
        return context;
    }
}
