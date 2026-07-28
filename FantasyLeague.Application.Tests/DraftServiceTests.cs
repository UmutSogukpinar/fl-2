using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Moq;

namespace FantasyLeague.Application.Tests;

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
        _leagueSetupRepository.Setup(repository => repository.AddAsync(
                It.IsAny<IReadOnlyCollection<LeagueFixture>>(),
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
    }

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
        _leagueSetupRepository.Verify(repository => repository.AddAsync(
            It.IsAny<IReadOnlyCollection<LeagueFixture>>(),
            It.IsAny<IReadOnlyCollection<DraftPickOrder>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

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

    [Fact]
    public async Task MakePickAsync_OnFinalPick_AddsPlayerAndActivatesLeague()
    {
        var league = CreateLeague(LeagueStatus.Drafting);
        var team = new FantasyTeam
        {
            Id = Guid.NewGuid(), LeagueId = league.Id, OwnerId = Guid.NewGuid(), Name = "Team"
        };
        var nbaPlayerId = Guid.NewGuid();
        var currentPick = CreatePick(league.Id, team.Id);
        var pendingResponse = CreatePickResponse(league.Id, team.Id);
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
            .ReturnsAsync([pendingResponse])
            .ReturnsAsync([completedResponse]);
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
    }

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
