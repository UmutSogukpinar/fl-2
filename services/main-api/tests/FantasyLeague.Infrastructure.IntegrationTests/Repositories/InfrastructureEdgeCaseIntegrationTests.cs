using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;
using FantasyLeague.Domain.Entities.Leagues;
using FantasyLeague.Domain.Entities.Players;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Infrastructure.IntegrationTests.Database;
using FantasyLeague.Infrastructure.Repositories.Drafts;
using FantasyLeague.Infrastructure.Repositories.FantasyTeams;
using FantasyLeague.Infrastructure.Repositories.Leagues;
using FantasyLeague.Infrastructure.Repositories.NbaPlayers;
using FantasyLeague.Infrastructure.Repositories.Users;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.IntegrationTests.Repositories;

[Collection(PostgreSqlCollection.Name)]
public sealed class InfrastructureEdgeCaseIntegrationTests(PostgreSqlFixture database)
    : IAsyncLifetime
{
    public Task InitializeAsync() => database.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UserSchema_WhenUsernameDuplicated_RejectsSecondUser()
    {
        await using var context = database.CreateContext();
        context.Add(IntegrationTestData.CreateUser("duplicate-user"));
        await context.SaveChangesAsync();
        context.Add(new FantasyLeague.Domain.Entities.Users.User
        {
            Username = "duplicate-user", Email = "other@example.com", Password = "hash"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UserSchema_WhenEmailDuplicated_RejectsSecondUser()
    {
        await using var context = database.CreateContext();
        context.Add(IntegrationTestData.CreateUser("first-email"));
        await context.SaveChangesAsync();
        context.Add(new FantasyLeague.Domain.Entities.Users.User
        {
            Username = "different-user", Email = "first-email@example.com", Password = "hash"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UserSchema_WhenUsernameExceedsMaximumLength_RejectsValue()
    {
        await using var context = database.CreateContext();
        context.Add(new FantasyLeague.Domain.Entities.Users.User
        {
            Username = new string('x', 51), Email = "long@example.com", Password = "hash"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task UserSchema_WhenEmailExceedsMaximumLength_RejectsValue()
    {
        await using var context = database.CreateContext();
        context.Add(new FantasyLeague.Domain.Entities.Users.User
        {
            Username = "long-email-user",
            Email = $"{new string('a', 244)}@example.com",
            Password = "hash"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task NbaPlayerSchema_WhenNbaIdDuplicated_RejectsSecondPlayer()
    {
        await using var context = database.CreateContext();
        context.AddRange(
            IntegrationTestData.CreatePlayer(6001, "First"),
            IntegrationTestData.CreatePlayer(6001, "Second"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task PlayerStatsSchema_WhenSeasonDuplicatedForPlayer_RejectsSecondRow()
    {
        await using var context = database.CreateContext();
        var player = IntegrationTestData.CreatePlayer(6002);
        context.AddRange(player,
            new PlayerStats { NbaPlayerId = player.Id, Season = 2026 });
        await context.SaveChangesAsync();

        await using var secondContext = database.CreateContext();
        secondContext.Add(new PlayerStats { NbaPlayerId = player.Id, Season = 2026 });

        await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task PlayerStatsSchema_WhenPlayerDoesNotExist_RejectsOrphanRow()
    {
        await using var context = database.CreateContext();
        context.Add(new PlayerStats { NbaPlayerId = Guid.NewGuid(), Season = 2026 });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task LeagueSchema_WhenJoinCodeDuplicated_RejectsSecondLeague()
    {
        await using var context = database.CreateContext();
        var (commissioner, owner, first) = await IntegrationTestData.AddLeagueAsync(context);
        context.Add(new League
        {
            Name = "Other League", Season = 2026, CommissionerId = owner.Id,
            JoinCode = first.JoinCode, Settings = new LeagueSettings()
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FantasyTeamSchema_WhenNameDuplicatedWithinLeague_RejectsSecondTeam()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("duplicate-team-owner");
        context.AddRange(owner2,
            Team("Same Name", league.Id, owner.Id),
            Team("Same Name", league.Id, owner2.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FantasyTeamSchema_WhenOwnerHasTwoTeamsInLeague_RejectsSecondTeam()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        context.AddRange(
            Team("First Team", league.Id, owner.Id),
            Team("Second Team", league.Id, owner.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task RosterSchema_WhenPlayerAssignedToTwoTeamsInLeague_RejectsSecondAssignment()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("roster-owner-two");
        var first = Team("First Roster", league.Id, owner.Id);
        var second = Team("Second Roster", league.Id, owner2.Id);
        var player = IntegrationTestData.CreatePlayer(6003);
        context.AddRange(owner2, first, second, player,
            Roster(first, league.Id, player.Id), Roster(second, league.Id, player.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task RosterSchema_WhenTeamAndLeagueDoNotMatch_RejectsAssignment()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = Team("Mismatched Team", league.Id, owner.Id);
        var player = IntegrationTestData.CreatePlayer(6004);
        context.AddRange(team, player);
        await context.SaveChangesAsync();
        context.Add(Roster(team, Guid.NewGuid(), player.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task DraftSchema_WhenOverallPickDuplicated_RejectsSecondPick()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = Team("Draft Constraint Team", league.Id, owner.Id);
        context.AddRange(team,
            Pick(league.Id, team.Id, 1, 1, 1),
            Pick(league.Id, team.Id, 2, 1, 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task FantasyTeamRepository_ReleaseMissingPlayer_ThrowsNotFound()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var team = Team("Empty Roster", league.Id, owner.Id);
        context.Add(team);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new FantasyTeamRepository(context).ReleaseAPlayerAsync(
                team.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task FantasyTeamRepository_AddAssignedPlayer_ThrowsConflict()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("conflict-roster-owner");
        var first = Team("Assigned Team", league.Id, owner.Id);
        var second = Team("Receiving Team", league.Id, owner2.Id);
        var player = IntegrationTestData.CreatePlayer(6005);
        context.AddRange(owner2, first, second, player, Roster(first, league.Id, player.Id));
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            new FantasyTeamRepository(context).AddPlayerFromPoolAsync(
                second.Id, player.Id, CancellationToken.None));
    }

    [Fact]
    public async Task FantasyTeamRepository_WhenAllEntitiesMissing_ReturnsEveryValidationFlag()
    {
        await using var context = database.CreateContext();

        var result = await new FantasyTeamRepository(context)
            .ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.True(result.HasFlag(TradeValidationResult.HomeTeamNotFound));
        Assert.True(result.HasFlag(TradeValidationResult.AwayTeamNotFound));
        Assert.True(result.HasFlag(TradeValidationResult.PlayerNotFound));
    }

    [Fact]
    public async Task FantasyTeamRepository_ApproveTransfer_WithSingleServiceCommit_SwapsRosters()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        league.Settings.RosterSize = 1;
        var owner2 = IntegrationTestData.CreateUser("transfer-commit-owner");
        var first = Team("Transfer First", league.Id, owner.Id);
        var second = Team("Transfer Second", league.Id, owner2.Id);
        var firstPlayer = IntegrationTestData.CreatePlayer(6010, "FirstTrade");
        var secondPlayer = IntegrationTestData.CreatePlayer(6011, "SecondTrade");
        context.AddRange(
            owner2, first, second, firstPlayer, secondPlayer,
            Roster(first, league.Id, firstPlayer.Id),
            Roster(second, league.Id, secondPlayer.Id));
        await context.SaveChangesAsync();
        var repository = new FantasyTeamRepository(context);
        var transferId = await repository.CreateTransferAsync(
            first.Id, second.Id, [firstPlayer.Id], [secondPlayer.Id],
            CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        await repository.ApproveTransferAsync(
            transferId, second.Id, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        context.ChangeTracker.Clear();

        var firstRoster = await context.Set<FantasyTeamPlayer>()
            .Where(player => player.FantasyTeamId == first.Id)
            .Select(player => player.NbaPlayerId).ToArrayAsync();
        var secondRoster = await context.Set<FantasyTeamPlayer>()
            .Where(player => player.FantasyTeamId == second.Id)
            .Select(player => player.NbaPlayerId).ToArrayAsync();

        Assert.Equal(secondPlayer.Id, Assert.Single(firstRoster));
        Assert.Equal(firstPlayer.Id, Assert.Single(secondRoster));
    }

    [Fact]
    public async Task DraftRepository_WhenNoPlayersExist_ReturnsNoAvailablePlayer()
    {
        await using var context = database.CreateContext();

        var playerId = await new DraftRepository(context).GetFirstAvailablePlayerIdAsync(
            Guid.NewGuid(), CancellationToken.None);

        Assert.Null(playerId);
    }

    [Fact]
    public async Task Repositories_WhenIdsDoNotExist_ReturnNullResponses()
    {
        await using var context = database.CreateContext();
        var id = Guid.NewGuid();

        Assert.Null(await new UserRepository(context).GetResponseByIdAsync(id, CancellationToken.None));
        Assert.Null(await new LeagueRepository(context).GetResponseByIdAsync(id, CancellationToken.None));
        Assert.Null(await new FantasyTeamRepository(context).GetResponseByIdAsync(id, CancellationToken.None));
        Assert.Null(await new NbaPlayerRepository(context).GetByIdAndSeasonAsync(
            id, 2026, Application.Models.PlayerResponseSize.Basic, CancellationToken.None));
    }

    [Fact]
    public async Task LeagueSetupRepository_DueFixtures_ExcludesFutureCancelledAndNullTimeGames()
    {
        await using var context = database.CreateContext();
        var (_, owner, league) = await IntegrationTestData.AddLeagueAsync(context);
        var owner2 = IntegrationTestData.CreateUser("fixture-edge-owner");
        var owner3 = IntegrationTestData.CreateUser("fixture-edge-owner-three");
        var home = Team("Edge Home", league.Id, owner.Id);
        var away = Team("Edge Away", league.Id, owner2.Id);
        var third = Team("Edge Third", league.Id, owner3.Id);
        context.AddRange(owner2, owner3, home, away, third);
        await context.SaveChangesAsync();
        context.AddRange(
            Fixture(league.Id, home.Id, away.Id, null, MatchStatus.Scheduled),
            Fixture(league.Id, away.Id, home.Id, DateTime.UtcNow.AddHours(1), MatchStatus.Scheduled),
            Fixture(league.Id, home.Id, third.Id, DateTime.UtcNow.AddMinutes(-1), MatchStatus.Cancelled, 2));
        await context.SaveChangesAsync();

        var due = await new LeagueSetupRepository(context).GetDueFixturesAsync(
            DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(due);
    }

    private static FantasyTeam Team(string name, Guid leagueId, Guid ownerId) => new()
    { Name = name, LeagueId = leagueId, OwnerId = ownerId };

    private static FantasyTeamPlayer Roster(FantasyTeam team, Guid leagueId, Guid playerId) => new()
    { FantasyTeamId = team.Id, LeagueId = leagueId, NbaPlayerId = playerId };

    private static DraftPickOrder Pick(
        Guid leagueId, Guid teamId, int round, int position, int overall) => new()
    {
        LeagueId = leagueId, TeamId = teamId, Round = round,
        PositionInRound = position, OverallPick = overall
    };

    private static LeagueFixture Fixture(
        Guid leagueId, Guid homeId, Guid awayId, DateTime? time,
        MatchStatus status, int week = 1) => new()
    {
        LeagueId = leagueId, HomeTeamId = homeId, AwayTeamId = awayId,
        Week = week, GameTime = time, Status = status
    };
}
