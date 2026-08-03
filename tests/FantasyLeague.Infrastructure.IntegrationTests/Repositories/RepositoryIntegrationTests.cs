using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.IntegrationTests.Database;
using FantasyLeague.Infrastructure.Repositories;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.IntegrationTests.Repositories;

[Collection(PostgreSqlCollection.Name)]
public sealed class RepositoryIntegrationTests(PostgreSqlFixture database) : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // Case: User repository persists and queries a user
    // Reasoning: Repository writes and normalized email queries should work against the real PostgreSQL schema.
    // Expected Result: The persisted user is returned with its projected response.
    [Fact]
    public async Task UserRepository_WhenUserIsSaved_ReturnsUserByEmailAndId()
    {
        await using var context = database.CreateContext();
        var repository = new UserRepository(context);
        var user = IntegrationTestData.CreateUser("repository-user");

        repository.Add(user);
        await repository.SaveChangesAsync(CancellationToken.None);

        var byEmail = await repository.GetByEmailAsync(
            " REPOSITORY-USER@EXAMPLE.COM ", CancellationToken.None);
        var response = await repository.GetResponseByIdAsync(
            user.Id, CancellationToken.None);

        Assert.Equal(user.Id, byEmail?.Id);
        Assert.Equal("repository-user", response?.Username);
    }

    // Case: League repository queries an overdue draft
    // Reasoning: Date and status filters should be translated and evaluated by PostgreSQL.
    // Expected Result: Only the eligible league is returned as due for draft.
    [Fact]
    public async Task LeagueRepository_WhenDraftDatePassed_ReturnsDueLeague()
    {
        await using var context = database.CreateContext();
        var (_, _, league) = await IntegrationTestData.AddLeagueAsync(
            context,
            LeagueStatus.RegistrationOpen,
            DateTime.UtcNow.AddMinutes(-5));
        var repository = new LeagueRepository(context);

        var due = await repository.GetDueForDraftAsync(
            DateTime.UtcNow, CancellationToken.None);
        var response = await repository.GetResponseByJoinCodeAsync(
            league.JoinCode, CancellationToken.None);

        Assert.Equal(league.Id, Assert.Single(due).Id);
        Assert.Equal(league.Id, response?.Id);
    }

    // Case: Fantasy team repository detects persisted conflicts
    // Reasoning: Owner and name uniqueness checks should run using real relational data.
    // Expected Result: Both owner and name conflict flags are returned.
    [Fact]
    public async Task FantasyTeamRepository_WhenOwnerAndNameExist_ReturnsBothConflicts()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = new FantasyTeam
        {
            Name = "Existing Team",
            LeagueId = league.Id,
            OwnerId = owner.Id
        };
        context.Add(team);
        await context.SaveChangesAsync();
        var repository = new FantasyTeamRepository(context);

        var conflict = await repository.ExistsAsync(
            league.Id,
            owner.Id,
            team.Name,
            null,
            CancellationToken.None);

        Assert.True(conflict.HasFlag(FastasyTeamConflictResult.OwnerHasMultipleTeam));
        Assert.True(conflict.HasFlag(FastasyTeamConflictResult.NameIsTaken));
        Assert.Equal(1, await repository.CountByLeagueIdAsync(
            league.Id, CancellationToken.None));
    }

    // Case: NBA player repository returns extended season data
    // Reasoning: Player and season-stat projections should execute against PostgreSQL.
    // Expected Result: The extended response contains the requested season statistics.
    [Fact]
    public async Task NbaPlayerRepository_WhenSeasonStatsExist_ReturnsExtendedPlayer()
    {
        await using var context = database.CreateContext();
        var player = IntegrationTestData.CreatePlayer(1001, "Extended");
        player.SeasonStats.Add(new PlayerStats
        {
            NbaPlayerId = player.Id,
            Season = 2026,
            GamesPlayed = 10,
            PointsPerGame = 24.5
        });
        context.Add(player);
        await context.SaveChangesAsync();
        var repository = new NbaPlayerRepository(context);

        var response = await repository.GetByIdAndSeasonAsync(
            player.Id,
            2026,
            PlayerResponseSize.Extended,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(player.Id, response.Id);
    }

    // Case: Draft repository returns the current unpicked order
    // Reasoning: Draft ordering and available-player queries should use persisted league data.
    // Expected Result: The first unpicked order and an undrafted player are returned.
    [Fact]
    public async Task DraftRepository_WhenDraftHasOpenPick_ReturnsCurrentPickAndPlayer()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(
            context, LeagueStatus.Drafting);
        var team = new FantasyTeam
        {
            Name = "Draft Team",
            LeagueId = league.Id,
            OwnerId = owner.Id
        };
        var player = IntegrationTestData.CreatePlayer(1002, "Available");
        var pick = new DraftPickOrder
        {
            LeagueId = league.Id,
            TeamId = team.Id,
            Round = 1,
            PositionInRound = 1,
            OverallPick = 1
        };
        context.AddRange(team, player, pick);
        await context.SaveChangesAsync();
        var repository = new DraftRepository(context);

        var current = await repository.GetCurrentTrackedPickAsync(
            league.Id, CancellationToken.None);
        var availablePlayerId = await repository.GetFirstAvailablePlayerIdAsync(
            league.Id, CancellationToken.None);

        Assert.Equal(pick.Id, current?.Id);
        Assert.Equal(player.Id, availablePlayerId);
    }

    // Case: League setup repository calculates persisted standings
    // Reasoning: Fixture joins and standing calculations should use the relational league setup.
    // Expected Result: The winning team is ranked first with three points.
    [Fact]
    public async Task LeagueSetupRepository_WhenFixtureCompleted_ReturnsStandings()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var secondOwner = IntegrationTestData.CreateUser("second-owner");
        var home = new FantasyTeam
        {
            Name = "Home Team",
            LeagueId = league.Id,
            OwnerId = owner.Id
        };
        var away = new FantasyTeam
        {
            Name = "Away Team",
            LeagueId = league.Id,
            OwnerId = secondOwner.Id
        };
        var fixture = new LeagueFixture
        {
            LeagueId = league.Id,
            Week = 1,
            HomeTeamId = home.Id,
            AwayTeamId = away.Id,
            HomeScore = 100,
            AwayScore = 90,
            GameTime = DateTime.UtcNow.AddMinutes(-1),
            Status = MatchStatus.Completed
        };
        context.AddRange(secondOwner, home, away, fixture);
        await context.SaveChangesAsync();
        var repository = new LeagueSetupRepository(context);

        var standings = await repository.GetStandingsAsync(
            league.Id, CancellationToken.None);

        Assert.Equal(home.Id, standings[0].TeamId);
        Assert.Equal(3, standings[0].Points);
    }
}
