using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests;

public sealed class FantasyTeamServiceTests
{
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly FantasyTeamService _service;

    public FantasyTeamServiceTests()
    {
        _service = new FantasyTeamService(
            _teamRepository.Object,
            _leagueRepository.Object,
            _userRepository.Object);
    }

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

    [Fact]
    public async Task CreateAsync_NormalizesMapsAndPersistsTeam()
    {
        var league = CreateLeague();
        var owner = CreateUser("owner");
        var request = new CreateFantasyTeamRequest("  Winners  ", league.Id, owner.Id);
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
            .ReturnsAsync(false);

        FantasyTeam? addedTeam = null;
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Callback<FantasyTeam, CancellationToken>((team, _) => addedTeam = team)
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(addedTeam);
        Assert.Equal("winners", addedTeam.Name);
        Assert.Equal(owner.Id, addedTeam.OwnerId);
        Assert.Equal(addedTeam.Id, response.Id);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenLeagueIsFull_ThrowsConflictException()
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

        var request = new CreateFantasyTeamRequest("Team", league.Id, owner.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(request, CancellationToken.None));
        _teamRepository.Verify(
            repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenTeamIsNotUnique_ThrowsConflictException()
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
            .ReturnsAsync(true);

        var request = new CreateFantasyTeamRequest("Team", league.Id, owner.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _service.CreateAsync(request, CancellationToken.None));
    }

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
            .ReturnsAsync(false);

        var response = await _service.UpdateAsync(
            team.Id,
            new UpdateFantasyTeamRequest("  Updated  "),
            CancellationToken.None);

        Assert.Equal("updated", team.Name);
        Assert.Equal("updated", response.Name);
        Assert.NotNull(team.UpdatedAt);
        _teamRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenTeamDoesNotExist_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _teamRepository
            .Setup(repository => repository.GetTrackedByIdAsync(
                id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FantasyTeam?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(id, CancellationToken.None));
        _teamRepository.Verify(
            repository => repository.Remove(It.IsAny<FantasyTeam>()), Times.Never);
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
}
