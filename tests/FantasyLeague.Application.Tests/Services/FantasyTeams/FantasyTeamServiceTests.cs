using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests.Services.FantasyTeams;

public sealed class FantasyTeamServiceTests
{
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<ILeagueSetupRepository> _leagueSetupRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly FantasyTeamService _service;

    public FantasyTeamServiceTests()
    {
        _service = new FantasyTeamService(
            _teamRepository.Object,
            _leagueRepository.Object,
            _leagueSetupRepository.Object,
            _userRepository.Object);
        _teamRepository
            .Setup(repository => repository.GetRosterStateAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((8, 13));
    }

    // Case: Get By League Id
    // Reasoning: This test verifies the Get By League Id operation.
    // Expected Result: The expected outcome is: Maps Teams To Responses.
    [Fact]
    public async Task GetByLeagueIdAsync_MapsTeamsToResponses()
    {
        var league = CreateLeague();
        var team = CreateTeam(league);
        SetupLeague(league);
        _teamRepository
            .Setup(repository => repository.GetPagedByLeagueIdAsync(
                league.Id, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([CreateTeamResponse(team)], 1));

        var result = await _service.GetByLeagueIdAsync(
            league.Id, new PaginationRequest());

        var response = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(team.Id, response.Id);
        Assert.Equal(team.Name, response.Name);
        Assert.Equal(league.Id, response.LeagueId);
    }

    // Case: Add League Member
    // Reasoning: This test verifies the Add League Member operation.
    // Expected Result: The expected outcome is: Normalizes Maps And Persists Team.
    [Fact]
    public async Task AddLeagueMemberAsync_NormalizesMapsAndPersistsTeam()
    {
        var league = CreateLeague();
        var owner = CreateUser("owner");
        var request = new AddLeagueMemberRequest("  Winners  ", owner.Id);
        SetupLeague(league);
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(owner));
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                league.Id, owner.Id, "winners", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FastasyTeamConflictResult.None);

        FantasyTeam? addedTeam = null;
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Callback<FantasyTeam, CancellationToken>((team, _) => addedTeam = team)
            .Returns(Task.CompletedTask);

        var response = await _service.AddLeagueMemberAsync(league.Id, request);

        Assert.NotNull(addedTeam);
        Assert.Equal("winners", addedTeam.Name);
        Assert.Equal(owner.Id, addedTeam.OwnerId);
        Assert.Equal(addedTeam.Id, response.Id);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Case: Add League Member when League Is Full
    // Reasoning: This test verifies Add League Member under the League Is Full condition.
    // Expected Result: The expected outcome is: Throws Conflict Exception.
    [Fact]
    public async Task AddLeagueMemberAsync_WhenLeagueIsFull_ThrowsConflictException()
    {
        var league = CreateLeague(maxTeams: 2);
        var owner = CreateUser("owner");
        SetupLeague(league);
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(owner));
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var request = new AddLeagueMemberRequest("Team", owner.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.AddLeagueMemberAsync(league.Id, request));
        _teamRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Case: Add League Member when Team Is Not Unique
    // Reasoning: This test verifies Add League Member under the Team Is Not Unique condition.
    // Expected Result: The expected outcome is: Throws Specific Conflict Exception.
    [Theory]
    [InlineData(
        FastasyTeamConflictResult.OwnerHasMultipleTeam,
        "The owner already has a team in this league.")]
    [InlineData(
        FastasyTeamConflictResult.NameIsTaken,
        "The team name is already used in this league.")]
    [InlineData(
        FastasyTeamConflictResult.OwnerHasMultipleTeam | FastasyTeamConflictResult.NameIsTaken,
        "The owner already has a team and the team name is already used in this league.")]
    public async Task AddLeagueMemberAsync_WhenTeamIsNotUnique_ThrowsSpecificConflictException(
        FastasyTeamConflictResult conflict,
        string expectedMessage)
    {
        var league = CreateLeague();
        var owner = CreateUser("owner");
        SetupLeague(league);
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(owner));
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                league.Id, owner.Id, "team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflict);

        var request = new AddLeagueMemberRequest("Team", owner.Id);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.AddLeagueMemberAsync(league.Id, request));

        Assert.Equal(expectedMessage, exception.Message);
        _teamRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Case: Update
    // Reasoning: This test verifies the Update operation.
    // Expected Result: The expected outcome is: Maps And Persists Team.
    [Fact]
    public async Task UpdateAsync_MapsAndPersistsTeam()
    {
        var league = CreateLeague();
        var team = CreateTeam(league);
        _teamRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                team.LeagueId,
                team.OwnerId,
                "updated",
                team.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FastasyTeamConflictResult.None);

        var response = await _service.UpdateAsync(
            team.Id,
            new UpdateFantasyTeamRequest("  Updated  ")
            );

        Assert.Equal("updated", team.Name);
        Assert.Equal("updated", response.Name);
        Assert.NotNull(team.UpdatedAt);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Case: Update when Name Is Taken
    // Reasoning: This test verifies Update under the Name Is Taken condition.
    // Expected Result: The expected outcome is: Does Not Modify Or Persist Team.
    [Fact]
    public async Task UpdateAsync_WhenNameIsTaken_DoesNotModifyOrPersistTeam()
    {
        var league = CreateLeague();
        var team = CreateTeam(league);
        var originalName = team.Name;
        var originalUpdatedAt = team.UpdatedAt;
        _teamRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                team.LeagueId,
                team.OwnerId,
                "taken name",
                team.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FastasyTeamConflictResult.NameIsTaken);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateAsync(
                team.Id,
                new UpdateFantasyTeamRequest("  Taken Name  ")
                ));

        Assert.Equal("The team name is already used in this league.", exception.Message);
        Assert.Equal(originalName, team.Name);
        Assert.Equal(originalUpdatedAt, team.UpdatedAt);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Case: Add League Member when League Is Missing
    // Reasoning: This test verifies Add League Member under the League Is Missing condition.
    // Expected Result: The expected outcome is: Does Not Query Owner.
    [Fact]
    public async Task AddLeagueMemberAsync_WhenLeagueIsMissing_DoesNotQueryOwner()
    {
        var leagueId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                leagueId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeagueResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AddLeagueMemberAsync(
                leagueId,
                new AddLeagueMemberRequest("Team", ownerId)));

        Assert.Equal($"League '{leagueId}' was not found.", exception.Message);
        _userRepository.Verify(repository => repository.GetResponseByIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyTeamWasNotAdded();
    }

    // Case: Add League Member when Owner Is Missing
    // Reasoning: This test verifies Add League Member under the Owner Is Missing condition.
    // Expected Result: The expected outcome is: Does Not Check Capacity.
    [Fact]
    public async Task AddLeagueMemberAsync_WhenOwnerIsMissing_DoesNotCheckCapacity()
    {
        var league = CreateLeague();
        var ownerId = Guid.NewGuid();
        SetupLeague(league);
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AddLeagueMemberAsync(
                league.Id,
                new AddLeagueMemberRequest("Team", ownerId)));

        Assert.Equal($"User '{ownerId}' was not found.", exception.Message);
        _teamRepository.Verify(repository => repository.CountByLeagueIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyTeamWasNotAdded();
    }

    // Case: Delete when Team Does Not Exist
    // Reasoning: This test verifies Delete under the Team Does Not Exist condition.
    // Expected Result: The expected outcome is: Throws Not Found Exception.
    [Fact]
    public async Task DeleteAsync_WhenTeamDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _teamRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FantasyTeam?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(id));
        _teamRepository.Verify(
            repository => repository.Remove(It.IsAny<FantasyTeam>()), Times.Never);
    }

    // Case: Release APlayer when Team And Player Exist
    // Reasoning: This test verifies Release APlayer under the Team And Player Exist condition.
    // Expected Result: The expected outcome is: Releases Player.
    [Fact]
    public async Task ReleaseAPlayerAsync_WhenTeamAndPlayerExist_ReleasesPlayer()
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId, null, playerId, cancellationToken))
            .ReturnsAsync(TradeValidationResult.None);
        _teamRepository
            .Setup(repository => repository.ReleaseAPlayerAsync(
                teamId, playerId, cancellationToken))
            .Returns(Task.CompletedTask);
        await _service.ReleaseAPlayerAsync(
            teamId, playerId, cancellationToken);

        _teamRepository.Verify(repository =>
            repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                teamId, null, playerId, cancellationToken), Times.Once);
        _teamRepository.Verify(repository => repository.ReleaseAPlayerAsync(
            teamId, playerId, cancellationToken), Times.Once);
    }

    // Case: Release APlayer when Release Would Drop Roster Below Half
    // Reasoning: This test verifies Release APlayer under the Release Would Drop Roster Below Half condition.
    // Expected Result: The expected outcome is: Throws Conflict.
    [Theory]
    [InlineData(7, 13, 7)]
    [InlineData(6, 12, 6)]
    public async Task ReleaseAPlayerAsync_WhenReleaseWouldDropRosterBelowHalf_ThrowsConflict(
        int playerCount,
        int rosterSize,
        int expectedMinimum)
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId, null, playerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeValidationResult.None);
        _teamRepository
            .Setup(repository => repository.GetRosterStateAsync(
                teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((playerCount, rosterSize));

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ReleaseAPlayerAsync(teamId, playerId));

        Assert.Equal(
            $"A player cannot be released because the roster must contain at least {expectedMinimum} players.",
            exception.Message);
        _teamRepository.Verify(repository => repository.ReleaseAPlayerAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Release APlayer when Validation Fails
    // Reasoning: This test verifies Release APlayer under the Validation Fails condition.
    // Expected Result: The expected outcome is: Throws Not Found And Does Not Release.
    [Theory]
    [InlineData(
        TradeValidationResult.HomeTeamNotFound,
        "Home fantasy team '{0}' was not found.")]
    [InlineData(
        TradeValidationResult.PlayerNotFound,
        "NBA player '{1}' was not found.")]
    [InlineData(
        TradeValidationResult.HomeTeamNotFound | TradeValidationResult.PlayerNotFound,
        "Home fantasy team '{0}' was not found. NBA player '{1}' was not found.")]
    [InlineData(
        TradeValidationResult.AwayTeamNotFound,
        "Away fantasy team was not found.")]
    [InlineData(
        TradeValidationResult.HomeTeamNotFound |
        TradeValidationResult.AwayTeamNotFound |
        TradeValidationResult.PlayerNotFound,
        "Home fantasy team '{0}' was not found. Away fantasy team was not found. " +
        "NBA player '{1}' was not found.")]
    public async Task ReleaseAPlayerAsync_WhenValidationFails_ThrowsNotFoundAndDoesNotRelease(
        TradeValidationResult validationResult,
        string expectedMessageFormat)
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId,
                    null,
                    playerId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ReleaseAPlayerAsync(teamId, playerId));

        Assert.Equal(
            string.Format(expectedMessageFormat, teamId, playerId),
            exception.Message);
        _teamRepository.Verify(repository => repository.ReleaseAPlayerAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Release APlayer when Validation Is Cancelled
    // Reasoning: This test verifies Release APlayer under the Validation Is Cancelled condition.
    // Expected Result: The expected outcome is: Does Not Release Player.
    [Fact]
    public async Task ReleaseAPlayerAsync_WhenValidationIsCancelled_DoesNotReleasePlayer()
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId, null, playerId, cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ReleaseAPlayerAsync(
                teamId, playerId, cancellationToken));

        Assert.Equal(cancellationToken, exception.CancellationToken);
        _teamRepository.Verify(repository => repository.ReleaseAPlayerAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Release APlayer when Player Is Not In Team
    // Reasoning: This test verifies Release APlayer under the Player Is Not In Team condition.
    // Expected Result: The expected outcome is: Propagates Repository Not Found.
    [Fact]
    public async Task ReleaseAPlayerAsync_WhenPlayerIsNotInTeam_PropagatesRepositoryNotFound()
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var expectedMessage =
            $"NBA player '{playerId}' was not found in fantasy team '{teamId}'.";
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId,
                    null,
                    playerId,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeValidationResult.None);
        _teamRepository
            .Setup(repository => repository.ReleaseAPlayerAsync(
                teamId, playerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException(expectedMessage));

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ReleaseAPlayerAsync(teamId, playerId));

        Assert.Equal(expectedMessage, exception.Message);
        _teamRepository.Verify(repository => repository.ReleaseAPlayerAsync(
            teamId, playerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Release APlayer when Release Is Cancelled
    // Reasoning: This test verifies Release APlayer under the Release Is Cancelled condition.
    // Expected Result: The expected outcome is: Propagates Cancellation.
    [Fact]
    public async Task ReleaseAPlayerAsync_WhenReleaseIsCancelled_PropagatesCancellation()
    {
        var teamId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    teamId, null, playerId, cancellationToken))
            .ReturnsAsync(TradeValidationResult.None);
        _teamRepository
            .Setup(repository => repository.ReleaseAPlayerAsync(
                teamId, playerId, cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ReleaseAPlayerAsync(
                teamId, playerId, cancellationToken));

        Assert.Equal(cancellationToken, exception.CancellationToken);
    }

    // Case: Create Transfer when With Valid Request
    // Reasoning: This test verifies Create Transfer under the With Valid Request condition.
    // Expected Result: The expected outcome is: Creates Request.
    [Fact]
    public async Task CreateTransferAsync_WithValidRequest_CreatesRequest()
    {
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        var homePlayerId = Guid.NewGuid();
        var awayPlayerId = Guid.NewGuid();
        var request = new CreateTransferRequest(
            awayTeamId, [homePlayerId], [awayPlayerId]);
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    homeTeamId, awayTeamId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeValidationResult.None);

        var transferId = Guid.NewGuid();
        _teamRepository.Setup(repository => repository.CreateTransferAsync(
            homeTeamId, awayTeamId, request.OfferedPlayerIds, request.RequestedPlayerIds,
            It.IsAny<CancellationToken>())).ReturnsAsync(transferId);

        var result = await _service.CreateTransferAsync(homeTeamId, request);

        Assert.Equal(transferId, result);
        _teamRepository.Verify(repository => repository.CreateTransferAsync(
            homeTeamId,
            awayTeamId,
            request.OfferedPlayerIds,
            request.RequestedPlayerIds,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Create Transfer when Teams Are Same
    // Reasoning: This test verifies Create Transfer under the Teams Are Same condition.
    // Expected Result: The expected outcome is: Throws Bad Request.
    [Fact]
    public async Task CreateTransferAsync_WhenTeamsAreSame_ThrowsBadRequest()
    {
        var teamId = Guid.NewGuid();
        var request = new CreateTransferRequest(
            teamId, [Guid.NewGuid()], [Guid.NewGuid()]);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateTransferAsync(teamId, request));

        Assert.Equal("A team cannot transfer players with itself.", exception.Message);
        _teamRepository.Verify(repository => repository.CreateTransferAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Create Transfer when Team Is Missing
    // Reasoning: This test verifies Create Transfer under the Team Is Missing condition.
    // Expected Result: The expected outcome is: Does Not Create Request.
    [Fact]
    public async Task CreateTransferAsync_WhenTeamIsMissing_DoesNotCreateRequest()
    {
        var homeTeamId = Guid.NewGuid();
        var request = new CreateTransferRequest(
            Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()]);
        _teamRepository
            .Setup(repository =>
                repository.ValidateExistenceOfFantasyTeamIdAndNbaPlayerId(
                    homeTeamId,
                    request.CounterpartyTeamId,
                    null,
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(TradeValidationResult.AwayTeamNotFound);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateTransferAsync(homeTeamId, request));

        _teamRepository.Verify(repository => repository.CreateTransferAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Approve Transfer when With Valid Identifiers
    // Reasoning: This test verifies Approve Transfer under the With Valid Identifiers condition.
    // Expected Result: The expected outcome is: Approves Request.
    [Fact]
    public async Task ApproveTransferAsync_WithValidIdentifiers_ApprovesRequest()
    {
        var transferId = Guid.NewGuid();
        var approvingTeamId = Guid.NewGuid();

        await _service.ApproveTransferAsync(transferId, approvingTeamId);

        _teamRepository.Verify(repository => repository.ApproveTransferAsync(
            transferId, approvingTeamId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Approve Transfer when With Empty Approving Team Id
    // Reasoning: This test verifies Approve Transfer under the With Empty Approving Team Id condition.
    // Expected Result: The expected outcome is: Throws Bad Request.
    [Fact]
    public async Task ApproveTransferAsync_WithEmptyApprovingTeamId_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ApproveTransferAsync(Guid.NewGuid(), Guid.Empty));

        _teamRepository.Verify(repository => repository.ApproveTransferAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupLeague(League league)
    {
        _leagueRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
    }

    private static User CreateUser(string username) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        Email = $"{username}@example.com",
        Password = "hash"
    };

    private static League CreateLeague(int maxTeams = 10)
    {
        var commissioner = CreateUser("commissioner");
        return new League
        {
            Id = Guid.NewGuid(),
            Name = "League",
            Season = 2026,
            MaxTeams = maxTeams,
            CommissionerId = commissioner.Id
        };
    }

    private static FantasyTeam CreateTeam(League league)
    {
        var owner = CreateUser("owner");
        return new FantasyTeam
        {
            Id = Guid.NewGuid(),
            Name = "Team",
            LeagueId = league.Id,
            OwnerId = owner.Id
        };
    }

    // Case: Add League Member when Last Team Joins
    // Reasoning: This test verifies Add League Member under the Last Team Joins condition.
    // Expected Result: The expected outcome is: Does Not Generate League Setup.
    [Fact]
    public async Task AddLeagueMemberAsync_WhenLastTeamJoins_DoesNotGenerateLeagueSetup()
    {
        var league = CreateLeague(maxTeams: 4);
        var owner = CreateUser("last-owner");
        SetupLeague(league);
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(owner));
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.AddLeagueMemberAsync(
            league.Id,
            new AddLeagueMemberRequest("Final Team", owner.Id));

        _leagueSetupRepository.Verify(repository => repository.AddDraftOrderAsync(
            It.IsAny<IReadOnlyCollection<DraftPickOrder>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _leagueSetupRepository.Verify(repository => repository.AddFixturesAsync(
            It.IsAny<IReadOnlyCollection<LeagueFixture>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Join League
    // Reasoning: This test verifies the Join League operation.
    // Expected Result: The expected outcome is: Resolves Code And Creates Team.
    [Fact]
    public async Task JoinLeagueAsync_ResolvesCodeAndCreatesTeam()
    {
        var league = CreateLeague();
        league.JoinCode = "ABC12345";
        var owner = CreateUser("owner");
        SetupLeague(league);
        _leagueRepository
            .Setup(repository => repository.GetResponseByJoinCodeAsync(
                "ABC12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLeagueResponse(league));
        _userRepository
            .Setup(repository => repository.GetResponseByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(owner));
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                league.Id, owner.Id, "new team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FastasyTeamConflictResult.None);
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _service.JoinLeagueAsync(
            new JoinLeagueRequest("  abc12345  ", "New Team", owner.Id));

        Assert.Equal(league.Id, response.LeagueId);
        Assert.Equal(owner.Id, response.OwnerId);
        Assert.Equal("new team", response.Name);
    }

    // Case: Join League when Without Owner Id
    // Reasoning: This test verifies Join League under the Without Owner Id condition.
    // Expected Result: The expected outcome is: Does Not Query League.
    [Fact]
    public async Task JoinLeagueAsync_WithoutOwnerId_DoesNotQueryLeague()
    {
        var request = new JoinLeagueRequest("ABC12345", "Team", Guid.Empty);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.JoinLeagueAsync(request));

        _leagueRepository.Verify(repository => repository.GetResponseByJoinCodeAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Case: Join League when Join Code Is Unknown
    // Reasoning: This test verifies Join League under the Join Code Is Unknown condition.
    // Expected Result: The expected outcome is: Does Not Create Team.
    [Fact]
    public async Task JoinLeagueAsync_WhenJoinCodeIsUnknown_DoesNotCreateTeam()
    {
        _leagueRepository
            .Setup(repository => repository.GetResponseByJoinCodeAsync(
                "UNKNOWN1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LeagueResponse?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.JoinLeagueAsync(new JoinLeagueRequest(
                "  unknown1  ",
                "Team",
                Guid.NewGuid())));

        Assert.Equal(
            "A league with the supplied join code was not found.",
            exception.Message);
        VerifyTeamWasNotAdded();
    }

    // Case: Remove League Member when Team Belongs To Another League
    // Reasoning: This test verifies Remove League Member under the Team Belongs To Another League condition.
    // Expected Result: The expected outcome is: Throws Not Found.
    [Fact]
    public async Task RemoveLeagueMemberAsync_WhenTeamBelongsToAnotherLeague_ThrowsNotFound()
    {
        var league = CreateLeague();
        var otherLeague = CreateLeague();
        var team = CreateTeam(otherLeague);
        SetupLeague(league);
        _teamRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.RemoveLeagueMemberAsync(league.Id, team.Id));
        _teamRepository.Verify(repository => repository.Remove(team), Times.Never);
    }

    private static FantasyTeamResponse CreateTeamResponse(FantasyTeam team) => new(
        team.Id,
        team.Name,
        team.LeagueId,
        team.OwnerId,
        team.CreatedAt,
        team.UpdatedAt);

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

    private void VerifyTeamWasNotAdded()
    {
        _teamRepository.Verify(repository => repository.AddAsync(
            It.IsAny<FantasyTeam>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _teamRepository.Verify(repository => repository.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
