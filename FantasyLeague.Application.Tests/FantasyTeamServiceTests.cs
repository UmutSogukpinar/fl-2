using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
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
            .Setup(repository => repository.GetByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        var result = await _service.GetByLeagueIdAsync(league.Id);

        var response = Assert.Single(result);
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
            .Setup(repository => repository.GetByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                league.Id, owner.Id, "Winners", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        FantasyTeam? addedTeam = null;
        _teamRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<FantasyTeam>(), It.IsAny<CancellationToken>()))
            .Callback<FantasyTeam, CancellationToken>((team, _) => addedTeam = team)
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(request);

        Assert.NotNull(addedTeam);
        Assert.Equal("Winners", addedTeam.Name);
        Assert.Same(owner, addedTeam.Owner);
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
            .Setup(repository => repository.GetByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var request = new CreateFantasyTeamRequest("Team", league.Id, owner.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));
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
            .Setup(repository => repository.GetByIdAsync(
                owner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                league.Id, owner.Id, "Team", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateFantasyTeamRequest("Team", league.Id, owner.Id);

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_MapsAndPersistsTeam()
    {
        var league = CreateLeague();
        var team = CreateTeam(league);
        _teamRepository
            .Setup(repository => repository.GetByIdAsync(
                team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _teamRepository
            .Setup(repository => repository.ExistsAsync(
                team.LeagueId,
                team.OwnerId,
                "Updated",
                team.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var response = await _service.UpdateAsync(
            team.Id, new UpdateFantasyTeamRequest("  Updated  "));

        Assert.Equal("Updated", team.Name);
        Assert.Equal("Updated", response.Name);
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
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FantasyTeam?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(id));
        _teamRepository.Verify(
            repository => repository.Remove(It.IsAny<FantasyTeam>()), Times.Never);
    }

    private void SetupLeague(League league)
    {
        _leagueRepository
            .Setup(repository => repository.GetByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);
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
            CommissionerId = commissioner.Id,
            Commissioner = commissioner
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
            OwnerId = owner.Id,
            Owner = owner
        };
    }
}
