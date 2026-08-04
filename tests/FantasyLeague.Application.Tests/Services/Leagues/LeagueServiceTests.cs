using Castle.Core.Logging;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace FantasyLeague.Application.Tests.Services.Leagues;

public sealed class LeagueServiceTests
{
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<ILeagueSetupRepository> _leagueSetupRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INbaPlayerRepository> _nbaPlayerRepository = new();
    private readonly Mock<ILogger<LeagueService>> _logger = new();
    private readonly LeagueService _service;

    public LeagueServiceTests()
    {
        _service = new LeagueService(
            _leagueRepository.Object,
            _teamRepository.Object,
            _leagueSetupRepository.Object,
            _userRepository.Object,
            _nbaPlayerRepository.Object,
            _logger.Object
            );
    }

    // Case: Get All
    // Reasoning: This test verifies the Get All operation.
    // Expected Result: The expected outcome is: Maps Leagues To Responses.
    [Fact]
    public async Task GetAllAsync_MapsLeaguesToResponses()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetPagedAsync(
                It.Is<PaginationRequest>(pagination =>
                    pagination.PageNumber == 1 && pagination.PageSize == 10),
                null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreateLeagueResponse(league)], 1));

        var result = await _service.GetAsync(new PaginationRequest());

        var response = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(league.Id, response.Id);
        Assert.Equal(league.Name, response.Name);
        Assert.Equal(league.CommissionerId, response.CommissionerId);
    }

    // Case: Get
    // Reasoning: This test verifies the Get operation.
    // Expected Result: The expected outcome is: Forwards Status Filter.
    [Fact]
    public async Task GetAsync_ForwardsStatusFilter()
    {
        _leagueRepository
            .Setup(repository => repository.GetPagedAsync(
                It.IsAny<PaginationRequest>(), LeagueStatus.Completed,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<LeagueResponse>(), 0));

        var result = await _service.GetAsync(
            new PaginationRequest(), LeagueStatus.Completed);

        Assert.Empty(result.Items);
        _leagueRepository.Verify(repository => repository.GetPagedAsync(
            It.Is<PaginationRequest>(pagination =>
                pagination.PageNumber == 1 && pagination.PageSize == 10),
            LeagueStatus.Completed, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Case: Create
    // Reasoning: This test verifies the Create operation.
    // Expected Result: The expected outcome is: Normalizes Maps And Persists League.
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

    // Case: Get Match Stats
    // Reasoning: This test verifies the Get Match Stats operation.
    // Expected Result: The expected outcome is: Uses League Season And Team Ids.
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

    // Case: Process Due Fixtures
    // Reasoning: This test verifies the Process Due Fixtures operation.
    // Expected Result: The expected outcome is: Calculates Scores And Completes Fixture And League.
    [Fact]
    public async Task ProcessDueFixturesAsync_CalculatesScoresAndCompletesFixtureAndLeague()
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
                new TeamMatchStats(
                    fixture.HomeTeamId, league.Season,
                    1, 21, 21, 30,
                    10, 2, 3, 0, 0, 0, 0, 0, 0),
                new TeamMatchStats(
                    fixture.AwayTeamId, league.Season,
                    1, 21, 21, 30,
                    8, 2, 3, 0, 0, 0, 0, 0, 0)));
        _leagueSetupRepository
            .Setup(repository => repository.HasUnfinishedFixturesAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _leagueRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        Assert.Equal(MatchStatus.Scheduled, fixture.Status);

        var processed = await _service.ProcessDueFixturesAsync(utcNow);

        Assert.Equal(1, processed);
        Assert.Equal(81, fixture.HomeScore);
        Assert.Equal(71, fixture.AwayScore);
        Assert.Equal(MatchStatus.Completed, fixture.Status);
        Assert.Equal(LeagueStatus.Completed, league.Status);
        Assert.Equal(utcNow, league.UpdatedAt);
        _leagueSetupRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _leagueRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Process Due Fixtures when Player Stats Are Missing
    // Reasoning: This test verifies Process Due Fixtures under the Player Stats Are Missing condition.
    // Expected Result: The expected outcome is: Does Not Complete Fixture.
    [Fact]
    public async Task ProcessDueFixturesAsync_WhenPlayerStatsAreMissing_DoesNotCompleteFixture()
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

        var processed = await _service.ProcessDueFixturesAsync(utcNow);

        Assert.Equal(0, processed);
        Assert.Null(fixture.HomeScore);
        Assert.Null(fixture.AwayScore);
        Assert.Equal(MatchStatus.Scheduled, fixture.Status);
        _leagueSetupRepository.Verify(repository =>
            repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Get Match Stats when Teams Are Same
    // Reasoning: This test verifies Get Match Stats under the Teams Are Same condition.
    // Expected Result: The expected outcome is: Throws Bad Request.
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

    // Case: Get Match Stats when League Is Missing
    // Reasoning: This test verifies Get Match Stats under the League Is Missing condition.
    // Expected Result: The expected outcome is: Throws Not Found.
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

    // Case: Get Match Stats when Home Team Is Missing
    // Reasoning: This test verifies Get Match Stats under the Home Team Is Missing condition.
    // Expected Result: The expected outcome is: Throws Not Found.
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

    // Case: Get Match Stats when Home Team Belongs To Another League
    // Reasoning: This test verifies Get Match Stats under the Home Team Belongs To Another League condition.
    // Expected Result: The expected outcome is: Throws Not Found.
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

    // Case: Get Match Stats when Away Team Is Missing
    // Reasoning: This test verifies Get Match Stats under the Away Team Is Missing condition.
    // Expected Result: The expected outcome is: Throws Not Found.
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

    // Case: Get Match Stats
    // Reasoning: This test verifies the Get Match Stats operation.
    // Expected Result: The expected outcome is: Forwards Cancellation Token.
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

    // Case: Create when Without Commissioner Id
    // Reasoning: This test verifies Create under the Without Commissioner Id condition.
    // Expected Result: The expected outcome is: Does Not Query User.
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

    // Case: Create when Commissioner Does Not Exist
    // Reasoning: This test verifies Create under the Commissioner Does Not Exist condition.
    // Expected Result: The expected outcome is: Throws Not Found Exception.
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

    // Case: Update when Max Teams Is Below Current Count
    // Reasoning: This test verifies Update under the Max Teams Is Below Current Count condition.
    // Expected Result: The expected outcome is: Throws Conflict Exception.
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

    // Case: Delete
    // Reasoning: This test verifies the Delete operation.
    // Expected Result: The expected outcome is: Removes And Persists League.
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

    // Case: Delete when Requester Is Not Commissioner
    // Reasoning: This test verifies Delete under the Requester Is Not Commissioner condition.
    // Expected Result: The expected outcome is: Throws Forbidden Exception.
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

    // Case: Delete when Draft Is Active
    // Reasoning: This test verifies Delete under the Draft Is Active condition.
    // Expected Result: The expected outcome is: Throws Conflict Exception.
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

    // Case: Create when Draft Date Is Missing
    // Reasoning: This test verifies Create under the Draft Date Is Missing condition.
    // Expected Result: The expected outcome is: Throws Bad Request Exception.
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
