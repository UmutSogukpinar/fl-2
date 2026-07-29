using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Moq;

namespace FantasyLeague.Application.Tests;

public sealed class LeagueServiceTests
{
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<ILeagueSetupRepository> _leagueSetupRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INbaPlayerRepository> _nbaPlayerRepository = new();
    private readonly LeagueService _service;

    public LeagueServiceTests()
    {
        _service = new LeagueService(
            _leagueRepository.Object,
            _teamRepository.Object,
            _leagueSetupRepository.Object,
            _userRepository.Object,
            _nbaPlayerRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_MapsLeaguesToResponses()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetPagedAsync(
                1, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreateLeagueResponse(league)], 1));

        var result = await _service.GetAsync(new PaginationRequest());

        var response = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(league.Id, response.Id);
        Assert.Equal(league.Name, response.Name);
        Assert.Equal(league.CommissionerId, response.CommissionerId);
    }

    [Fact]
    public async Task GetAsync_ForwardsStatusFilter()
    {
        _leagueRepository
            .Setup(repository => repository.GetPagedAsync(
                1, 10, LeagueStatus.Completed, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<LeagueResponse>(), 0));

        var result = await _service.GetAsync(
            new PaginationRequest(), LeagueStatus.Completed);

        Assert.Empty(result.Items);
        _leagueRepository.Verify(repository => repository.GetPagedAsync(
            1, 10, LeagueStatus.Completed, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NormalizesMapsAndPersistsLeague()
    {
        var commissioner = CreateUser();
        var draftDate = DateTime.UtcNow.AddDays(7);
        var request = new CreateLeagueRequest(
            "  Champions  ",
            "  Main league  ",
            2026,
            12,
            commissioner.Id,
            draftDate,
            TeamName: "  Istanbul Ballers  ");

        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                commissioner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(commissioner));

        League? addedLeague = null;
        _leagueRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<League>(), It.IsAny<CancellationToken>()))
            .Callback<League, CancellationToken>((league, _) => addedLeague = league)
            .Returns(Task.CompletedTask);
        FantasyTeam? addedTeam = null;
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Callback<FantasyTeam, CancellationToken>((team, _) => addedTeam = team)
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(request);

        Assert.NotNull(addedLeague);
        Assert.Equal("Champions", addedLeague.Name);
        Assert.Equal("Main league", addedLeague.Description);
        Assert.Equal(commissioner.Id, addedLeague.CommissionerId);
        Assert.Equal(LeagueStatus.Created, addedLeague.Status);
        Assert.Equal(draftDate, addedLeague.Settings.DraftDate);
        Assert.Equal(13, addedLeague.Settings.RosterSize);
        Assert.Equal(8, addedLeague.JoinCode.Length);
        Assert.Equal(addedLeague.Id, response.Id);
        Assert.NotNull(addedTeam);
        Assert.Equal("Istanbul Ballers", addedTeam.Name);
        Assert.Equal(addedLeague.Id, addedTeam.LeagueId);
        Assert.Equal(commissioner.Id, addedTeam.OwnerId);
        _leagueRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMatchStatsAsync_UsesLeagueSeasonAndTeamIds()
    {
        var league = CreateLeague();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        var expected = new MatchStats(
            TeamMatchStats.Empty(homeTeamId, league.Season),
            TeamMatchStats.Empty(awayTeamId, league.Season));
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
        _teamRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                homeTeamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FantasyTeamResponse(
                homeTeamId,
                "Home",
                league.Id,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null));
        _teamRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                awayTeamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FantasyTeamResponse(
                awayTeamId,
                "Away",
                league.Id,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null));
        _nbaPlayerRepository
            .Setup(repository => repository.GetMatchStatsByTeamIdsAsync(
                league.Id,
                homeTeamId,
                awayTeamId,
                league.Season,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.GetMatchStatsAsync(
            league.Id,
            homeTeamId,
            awayTeamId);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ProcessDueFixturesAsync_WhenFinalFixtureCompletes_MarksLeagueCompleted()
    {
        var league = CreateLeague();
        league.Status = LeagueStatus.Active;
        var fixture = new LeagueFixture
        {
            LeagueId = league.Id,
            HomeTeamId = Guid.NewGuid(),
            AwayTeamId = Guid.NewGuid(),
            Week = 1,
            GameTime = DateTime.UtcNow.AddMinutes(-1)
        };
        var utcNow = DateTime.UtcNow;

        _leagueSetupRepository
            .Setup(repository => repository.GetDueFixturesAsync(
                utcNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync([fixture]);
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
        _nbaPlayerRepository
            .Setup(repository => repository.GetMatchStatsByTeamIdsAsync(
                league.Id, fixture.HomeTeamId, fixture.AwayTeamId,
                league.Season, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchStats(
                TeamMatchStats.Empty(fixture.HomeTeamId, league.Season),
                TeamMatchStats.Empty(fixture.AwayTeamId, league.Season)));
        _leagueSetupRepository
            .Setup(repository => repository.HasUnfinishedFixturesAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        var processed = await _service.ProcessDueFixturesAsync(utcNow);

        Assert.Equal(1, processed);
        Assert.Equal(LeagueStatus.Completed, league.Status);
        Assert.Equal(utcNow, league.UpdatedAt);
        Assert.NotNull(fixture.HomeScore);
        Assert.NotNull(fixture.AwayScore);
        _leagueSetupRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _leagueRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMatchStatsAsync_WhenTeamsAreSame_ThrowsBadRequest()
    {
        var teamId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.GetMatchStatsAsync(Guid.NewGuid(), teamId, teamId));

        Assert.Equal(
            "Home and away teams must be different.",
            exception.Message);
        _leagueRepository.Verify(repository =>
            repository.GetResponseByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyMatchStatsRepositoryWasNotCalled();
    }

    [Fact]
    public async Task GetMatchStatsAsync_WhenLeagueIsMissing_ThrowsNotFound()
    {
        var leagueId = Guid.NewGuid();
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                leagueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeagueResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetMatchStatsAsync(
                leagueId,
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Equal($"League '{leagueId}' was not found.", exception.Message);
        _teamRepository.Verify(repository =>
            repository.GetResponseByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyMatchStatsRepositoryWasNotCalled();
    }

    [Fact]
    public async Task GetMatchStatsAsync_WhenHomeTeamIsMissing_ThrowsNotFound()
    {
        var league = CreateLeague();
        var homeTeamId = Guid.NewGuid();
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
        _teamRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                homeTeamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FantasyTeamResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetMatchStatsAsync(
                league.Id,
                homeTeamId,
                Guid.NewGuid()));

        Assert.Contains("Home fantasy team", exception.Message);
        VerifyMatchStatsRepositoryWasNotCalled();
    }

    [Fact]
    public async Task GetMatchStatsAsync_WhenHomeTeamBelongsToAnotherLeague_ThrowsNotFound()
    {
        var league = CreateLeague();
        var homeTeamId = Guid.NewGuid();
        SetupLeagueResponse(league);
        SetupTeamResponse(homeTeamId, Guid.NewGuid(), "Home");

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetMatchStatsAsync(
                league.Id,
                homeTeamId,
                Guid.NewGuid()));

        Assert.Contains($"Home fantasy team '{homeTeamId}'", exception.Message);
        VerifyMatchStatsRepositoryWasNotCalled();
    }

    [Fact]
    public async Task GetMatchStatsAsync_WhenAwayTeamIsMissing_ThrowsNotFound()
    {
        var league = CreateLeague();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        SetupLeagueResponse(league);
        SetupTeamResponse(homeTeamId, league.Id, "Home");
        _teamRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                awayTeamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FantasyTeamResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetMatchStatsAsync(
                league.Id,
                homeTeamId,
                awayTeamId));

        Assert.Contains($"Away fantasy team '{awayTeamId}'", exception.Message);
        VerifyMatchStatsRepositoryWasNotCalled();
    }

    [Fact]
    public async Task GetMatchStatsAsync_ForwardsCancellationToken()
    {
        var league = CreateLeague();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var expected = new MatchStats(
            TeamMatchStats.Empty(homeTeamId, league.Season),
            TeamMatchStats.Empty(awayTeamId, league.Season));

        SetupLeagueResponse(league);
        SetupTeamResponse(homeTeamId, league.Id, "Home");
        SetupTeamResponse(awayTeamId, league.Id, "Away");
        _nbaPlayerRepository
            .Setup(repository => repository.GetMatchStatsByTeamIdsAsync(
                league.Id,
                homeTeamId,
                awayTeamId,
                league.Season,
                cancellationToken))
            .ReturnsAsync(expected);

        await _service.GetMatchStatsAsync(
            league.Id,
            homeTeamId,
            awayTeamId,
            cancellationToken);

        _nbaPlayerRepository.Verify(repository =>
            repository.GetMatchStatsByTeamIdsAsync(
                league.Id,
                homeTeamId,
                awayTeamId,
                league.Season,
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutCommissionerId_DoesNotQueryUser()
    {
        var request = new CreateLeagueRequest(
            "League",
            null,
            2026,
            10,
            Guid.Empty,
            DateTime.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(request));

        _userRepository.Verify(repository => repository.GetResponseByIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCommissionerDoesNotExist_ThrowsNotFoundException()
    {
        var request = new CreateLeagueRequest(
            "League", null, 2026, 10, Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                request.CommissionerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request));
        _leagueRepository.Verify(
            repository => repository.AddAsync(It.IsAny<League>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenMaxTeamsIsBelowCurrentCount_ThrowsConflictException()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        var request = new UpdateLeagueRequest(
            "Updated", null, 7, DateTime.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(league.Id, request));
        _leagueRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAndPersistsLeague()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        await _service.DeleteAsync(league.Id, league.CommissionerId);

        _leagueRepository.Verify(repository => repository.Remove(league), Times.Once);
        _leagueRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenRequesterIsNotCommissioner_ThrowsForbiddenException()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.DeleteAsync(league.Id, Guid.NewGuid()));

        _leagueRepository.Verify(repository => repository.Remove(It.IsAny<League>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenDraftIsActive_ThrowsConflictException()
    {
        var league = CreateLeague();
        league.Status = LeagueStatus.Drafting;
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeleteAsync(league.Id, league.CommissionerId));

        _leagueRepository.Verify(repository => repository.Remove(It.IsAny<League>()), Times.Never);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "commissioner",
        Email = "commissioner@example.com",
        Password = "hash"
    };

    private static League CreateLeague()
    {
        var commissioner = CreateUser();
        return new League
        {
            Id = Guid.NewGuid(),
            Name = "League",
            Season = 2026,
            MaxTeams = 10,
            CommissionerId = commissioner.Id
        };
    }

    [Fact]
    public async Task CreateAsync_WhenDraftDateIsMissing_ThrowsBadRequestException()
    {
        var request = new CreateLeagueRequest(
            "League", null, 2026, 10, Guid.NewGuid(), default);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateAsync(request));
        _leagueRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<League>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static LeagueResponse CreateLeagueResponse(League league) => new(
        league.Id,
        league.Name,
        league.Description,
        league.Season,
        league.MaxTeams,
        league.CommissionerId,
        league.Status,
        league.Settings.DraftDate,
        league.JoinCode,
        league.CreatedAt,
        league.UpdatedAt);

    private static UserResponse CreateUserResponse(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.CreatedAt,
        user.UpdatedAt);

    private void SetupLeagueResponse(League league)
    {
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
    }

    private void SetupTeamResponse(Guid teamId, Guid leagueId, string name)
    {
        _teamRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FantasyTeamResponse(
                teamId,
                name,
                leagueId,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null));
    }

    private void VerifyMatchStatsRepositoryWasNotCalled()
    {
        _nbaPlayerRepository.Verify(repository =>
            repository.GetMatchStatsByTeamIdsAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
