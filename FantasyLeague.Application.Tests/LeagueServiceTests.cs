using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests;

public sealed class LeagueServiceTests
{
    private readonly Mock<ILeagueRepository> _leagueRepository = new();
    private readonly Mock<IFantasyTeamRepository> _teamRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly LeagueService _service;

    public LeagueServiceTests()
    {
        _service = new LeagueService(
            _leagueRepository.Object,
            _teamRepository.Object,
            _userRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_MapsLeaguesToResponses()
    {
        var league = CreateLeague();
        _leagueRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([league]);

        var result = await _service.GetAllAsync();

        var response = Assert.Single(result);
        Assert.Equal(league.Id, response.Id);
        Assert.Equal(league.Name, response.Name);
        Assert.Equal(league.CommissionerId, response.CommissionerId);
    }

    [Fact]
    public async Task CreateAsync_NormalizesMapsAndPersistsLeague()
    {
        var commissioner = CreateUser();
        var request = new CreateLeagueRequest(
            "  Champions  ", "  Main league  ", 2026, 12, commissioner.Id);

        _userRepository
            .Setup(repository => repository.GetByIdAsync(
                commissioner.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(commissioner);

        League? addedLeague = null;
        _leagueRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<League>(), It.IsAny<CancellationToken>()))
            .Callback<League, CancellationToken>((league, _) => addedLeague = league)
            .Returns(Task.CompletedTask);

        var response = await _service.CreateAsync(request);

        Assert.NotNull(addedLeague);
        Assert.Equal("Champions", addedLeague.Name);
        Assert.Equal("Main league", addedLeague.Description);
        Assert.Same(commissioner, addedLeague.Commissioner);
        Assert.Equal(addedLeague.Id, response.Id);
        _leagueRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCommissionerDoesNotExist_ThrowsNotFoundException()
    {
        var request = new CreateLeagueRequest("League", null, 2026, 10, Guid.NewGuid());

        _userRepository
            .Setup(repository => repository.GetByIdAsync(
                request.CommissionerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

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
            .Setup(repository => repository.GetByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);
        _teamRepository
            .Setup(repository => repository.CountByLeagueIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        var request = new UpdateLeagueRequest("Updated", null, 7);

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
            .Setup(repository => repository.GetByIdAsync(
                league.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(league);

        await _service.DeleteAsync(league.Id);

        _leagueRepository.Verify(repository => repository.Remove(league), Times.Once);
        _leagueRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
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
            CommissionerId = commissioner.Id,
            Commissioner = commissioner
        };
    }
}
