using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Moq;

namespace FantasyLeague.Application.Tests.Services.Drafts;

public sealed class DraftServiceTests
{
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<ILeagueSetupRepository> _leagueSetupRepository = new();
    private readonly Mock<IDraftRepository> _draftRepository = new();
    private readonly DraftService _service;

    public DraftServiceTests() =>
        _service = new DraftService(
            _leagueRepository.Object,
            _teamRepository.Object,
            _leagueSetupRepository.Object,
            _draftRepository.Object);

    // Case: Start Due Drafts when Draft Time Has Arrived
    // Reasoning: This test verifies Start Due Drafts under the Draft Time Has Arrived condition.
    // Expected Result: The expected outcome is: Starts Draft.
    [Fact]
    public async Task StartDueDraftsAsync_WhenDraftTimeHasArrived_StartsDraft()
    {
        var league = CreateLeague(LeagueStatus.RegistrationOpen);
        var utcNow = DateTime.UtcNow;
        _leagueRepository.Setup(repository => repository.GetDueForDraftAsync(
                utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([league]);
        _teamRepository.Setup(repository => repository.GetIdsByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.NewGuid(), Guid.NewGuid()]);
        _leagueSetupRepository.Setup(repository => repository.AddDraftOrderAsync(
                It.IsAny<IReadOnlyCollection<DraftPickOrder>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _draftRepository.Setup(repository => repository.GetPicksAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreatePickResponse(league.Id)]);

        var states = await _service.StartDueDraftsAsync(utcNow);

        var state = Assert.Single(states);
        Assert.Equal(LeagueStatus.Drafting, state.Status);
        Assert.Equal(LeagueStatus.Drafting, league.Status);
        Assert.Equal(utcNow, league.UpdatedAt);
        _leagueRepository.Verify(repository => repository.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        _leagueSetupRepository.Verify(repository => repository.AddFixturesAsync(
            It.IsAny<IReadOnlyCollection<LeagueFixture>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Start Due Drafts when With Fewer Than Two Teams
    // Reasoning: This test verifies Start Due Drafts under the With Fewer Than Two Teams condition.
    // Expected Result: The expected outcome is: Delays Draft.
    [Fact]
    public async Task StartDueDraftsAsync_WithFewerThanTwoTeams_DelaysDraft()
    {
        var league = CreateLeague(LeagueStatus.RegistrationOpen);
        var utcNow = DateTime.UtcNow;
        _leagueRepository.Setup(repository => repository.GetDueForDraftAsync(
                utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([league]);
        _teamRepository.Setup(repository => repository.GetIdsByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.NewGuid()]);

        var states = await _service.StartDueDraftsAsync(utcNow);

        Assert.Empty(states);
        Assert.Equal(LeagueStatus.DraftDelayed, league.Status);
        _leagueSetupRepository.Verify(repository => repository.AddDraftOrderAsync(
            It.IsAny<IReadOnlyCollection<DraftPickOrder>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Make Pick when It Is Another Teams Turn
    // Reasoning: This test verifies Make Pick under the It Is Another Teams Turn condition.
    // Expected Result: The expected outcome is: Throws Conflict.
    [Fact]
    public async Task MakePickAsync_WhenItIsAnotherTeamsTurn_ThrowsConflict()
    {
        var league = CreateLeague(LeagueStatus.Drafting);
        var currentPick = CreatePick(league.Id);
        SetupTrackedLeague(league);
        _draftRepository.Setup(repository => repository.GetCurrentTrackedPickAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPick);

        await Assert.ThrowsAsync<ConflictException>(() => _service.MakePickAsync(
            league.Id,
            new MakeDraftPickRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())));
    }

    // Case: Make Pick when With Null Request
    // Reasoning: This test verifies Make Pick under the With Null Request condition.
    // Expected Result: The expected outcome is: Does Not Query League.
    [Fact]
    public async Task MakePickAsync_WithNullRequest_DoesNotQueryLeague()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.MakePickAsync(Guid.NewGuid(), null!));

        _leagueRepository.Verify(repository => repository.GetTrackedByIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MakePickAsync_WhenPlayerIsAlreadyAssigned_ThrowsConflict()
    {
        var league = CreateLeague(LeagueStatus.Drafting);
        var team = new FantasyTeam
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            OwnerId = Guid.NewGuid(),
            Name = "Team"
        };
        var currentPick = CreatePick(league.Id, team.Id);
        var nbaPlayerId = Guid.NewGuid();

        SetupTrackedLeague(league);
        _draftRepository.Setup(repository => repository.GetCurrentTrackedPickAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPick);
        _draftRepository.Setup(repository => repository.GetTeamAsync(
                league.Id, team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _draftRepository.Setup(repository => repository.NbaPlayerExistsAsync(
                nbaPlayerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _draftRepository.Setup(repository => repository.IsPlayerUnavailableAsync(
                league.Id, nbaPlayerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.MakePickAsync(
            league.Id,
            new MakeDraftPickRequest(team.Id, team.OwnerId, nbaPlayerId)));

        _draftRepository.Verify(repository => repository.AddRosterPlayerAsync(
            It.IsAny<FantasyTeamPlayer>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Make Pick when On Final Pick
    // Reasoning: This test verifies Make Pick under the On Final Pick condition.
    // Expected Result: The expected outcome is: Adds Player And Activates League.
    [Fact]
    public async Task MakePickAsync_OnFinalPick_AddsPlayerAndActivatesLeague()
    {
        var league = CreateLeague(LeagueStatus.Drafting);
        var team = new FantasyTeam
        {
            Id = Guid.NewGuid(),
            LeagueId = league.Id,
            OwnerId = Guid.NewGuid(),
            Name = "Team"
        };
        var firstTeamId = Guid.NewGuid();
        var nbaPlayerId = Guid.NewGuid();
        var currentPick = CreatePick(league.Id, team.Id);
        currentPick.PositionInRound = 2;
        currentPick.OverallPick = 2;
        var firstPickResponse = new DraftPickResponse(
            Guid.NewGuid(),
            firstTeamId,
            "First Team",
            1,
            1,
            1,
            Guid.NewGuid(),
            "First Player",
            DateTime.UtcNow.AddMinutes(-1));
        var pendingResponse = new DraftPickResponse(
            currentPick.Id,
            team.Id,
            team.Name,
            1,
            2,
            2,
            null,
            null,
            null);
        var completedResponse = pendingResponse with
        {
            NbaPlayerId = nbaPlayerId,
            NbaPlayerName = "Test Player",
            PickedAt = DateTime.UtcNow
        };
        SetupTrackedLeague(league);
        _draftRepository.Setup(repository => repository.GetCurrentTrackedPickAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPick);
        _draftRepository.Setup(repository => repository.GetTeamAsync(
                league.Id, team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _draftRepository.Setup(repository => repository.NbaPlayerExistsAsync(
                nbaPlayerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _draftRepository.SetupSequence(repository => repository.GetPicksAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstPickResponse, pendingResponse])
            .ReturnsAsync([firstPickResponse, completedResponse]);
        _draftRepository.Setup(repository => repository.TrySaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var state = await _service.MakePickAsync(
            league.Id,
            new MakeDraftPickRequest(team.Id, team.OwnerId, nbaPlayerId));

        Assert.Equal(LeagueStatus.Active, league.Status);
        Assert.Equal(LeagueStatus.Active, state.Status);
        Assert.Null(state.CurrentPick);
        _draftRepository.Verify(repository => repository.AddRosterPlayerAsync(
            It.Is<FantasyTeamPlayer>(player =>
                player.FantasyTeamId == team.Id && player.NbaPlayerId == nbaPlayerId),
            It.IsAny<CancellationToken>()), Times.Once);
        _leagueSetupRepository.Verify(repository => repository.AddFixturesAsync(
            It.Is<IReadOnlyCollection<LeagueFixture>>(fixtures =>
                fixtures.Count == 1
                && fixtures.Single().LeagueId == league.Id
                && (fixtures.Single().HomeTeamId == firstTeamId
                    && fixtures.Single().AwayTeamId == team.Id
                    || fixtures.Single().HomeTeamId == team.Id
                    && fixtures.Single().AwayTeamId == firstTeamId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Auto Pick Expired when Deadline Passed
    // Reasoning: This test verifies Auto Pick Expired under the Deadline Passed condition.
    // Expected Result: The expected outcome is: Selects Available Player.
    [Fact]
    public async Task AutoPickExpiredAsync_WhenDeadlinePassed_SelectsAvailablePlayer()
    {
        var utcNow = DateTime.UtcNow;
        var league = CreateLeague(LeagueStatus.Drafting);
        league.UpdatedAt = utcNow.AddSeconds(-61);
        var currentPick = CreatePick(league.Id);
        var nbaPlayerId = Guid.NewGuid();
        var pendingResponse = new DraftPickResponse(
            currentPick.Id,
            currentPick.TeamId,
            "Team",
            1,
            1,
            1,
            null,
            null,
            null);
        var completedResponse = pendingResponse with
        {
            NbaPlayerId = nbaPlayerId,
            NbaPlayerName = "Auto Player",
            PickedAt = utcNow
        };

        _leagueRepository.Setup(repository => repository.GetDraftingAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([league]);
        _draftRepository.SetupSequence(repository => repository.GetPicksAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pendingResponse])
            .ReturnsAsync([completedResponse]);
        _draftRepository.Setup(repository => repository.GetCurrentTrackedPickAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPick);
        _draftRepository.Setup(repository => repository.GetFirstAvailablePlayerIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nbaPlayerId);
        _draftRepository.Setup(repository => repository.TrySaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var states = await _service.AutoPickExpiredAsync(utcNow);

        var state = Assert.Single(states);
        Assert.Equal(LeagueStatus.Active, state.Status);
        Assert.Equal(nbaPlayerId, currentPick.NbaPlayerId);
        Assert.Equal(utcNow, currentPick.PickedAt);
        _draftRepository.Verify(repository => repository.AddRosterPlayerAsync(
            It.Is<FantasyTeamPlayer>(player =>
                player.FantasyTeamId == currentPick.TeamId
                && player.NbaPlayerId == nbaPlayerId),
            It.IsAny<CancellationToken>()), Times.Once);
        _leagueSetupRepository.Verify(repository => repository.AddFixturesAsync(
            It.IsAny<IReadOnlyCollection<LeagueFixture>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutoPickExpiredAsync_OnFifthSystemFailure_CancelsDraft()
    {
        var utcNow = DateTime.UtcNow;
        var league = CreateLeague(LeagueStatus.Drafting);
        league.UpdatedAt = utcNow.AddSeconds(-61);
        league.ConsecutiveDraftFailureCount = 4;
        var currentPick = CreatePick(league.Id);
        var pendingResponse = CreatePickResponse(league.Id, currentPick.TeamId) with
        {
            Id = currentPick.Id
        };

        _leagueRepository.Setup(repository => repository.GetDraftingAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([league]);
        _draftRepository.Setup(repository => repository.GetPicksAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([pendingResponse]);
        _draftRepository.Setup(repository => repository.GetCurrentTrackedPickAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentPick);
        _draftRepository.Setup(repository => repository.GetFirstAvailablePlayerIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _draftRepository.Setup(repository => repository.TrySaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _leagueRepository.Setup(repository => repository.RecordDraftFailureAsync(
                league.Id, 5, utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var states = await _service.AutoPickExpiredAsync(utcNow);

        var state = Assert.Single(states);
        Assert.Equal(LeagueStatus.DraftCancelled, state.Status);
        _leagueRepository.Verify(repository => repository.RecordDraftFailureAsync(
            league.Id, 5, utcNow, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupTrackedLeague(League league) =>
        _leagueRepository.Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

    private static League CreateLeague(LeagueStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Name = "League",
        Season = 2026,
        CommissionerId = Guid.NewGuid(),
        Status = status
    };

    private static DraftPickOrder CreatePick(Guid leagueId, Guid? teamId = null) => new()
    {
        Id = Guid.NewGuid(),
        LeagueId = leagueId,
        TeamId = teamId ?? Guid.NewGuid(),
        Round = 1,
        PositionInRound = 1,
        OverallPick = 1
    };

    private static DraftPickResponse CreatePickResponse(Guid leagueId, Guid? teamId = null) =>
        new(Guid.NewGuid(), teamId ?? Guid.NewGuid(), "Team", 1, 1, 1, null, null, null);
}
