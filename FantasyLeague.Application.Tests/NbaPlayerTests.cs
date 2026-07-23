using Moq;
using Xunit;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Tests;

public class NbaPlayerTests
{
    private readonly Mock<INbaPlayerRepository> _playerRepository;
    private readonly NbaPlayerService _playerService;

    public NbaPlayerTests()
    {
        _playerRepository = new Mock<INbaPlayerRepository>();
        _playerService = new NbaPlayerService(_playerRepository.Object);
    }

    // ====================== Get Player Tests ======================

    // Case: Nba player found by Id
    // Reasoning: The service should return the player information
    // when a valid Id is provided.
    // Expected Result: The returned player information
    // should match the expected values.
    [Fact]
    public async Task doesFindBasicNbaPlayerById()
    {
        // Arrange
        var playerName = "Lebron";
        var playerLastName = "James";
        var playerTeam = "Lakers";
        var playerPosition = "F";

        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);

        _playerRepository.Setup(repo => repo.GetByIdAsync(
            playerId, PlayerResponseSize.Basic, It.IsAny<CancellationToken>())!
        )
        .ReturnsAsync(player);

        // Act
        var result = await _playerService.GetNbaPlayerAsync(
            playerId, PlayerResponseSize.Basic, CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playerId, result.Id);
        Assert.Equal(playerName, result.FirstName);
        Assert.Equal(playerLastName, result.LastName);
        Assert.Equal(playerTeam, result.Team);
        Assert.Equal(playerPosition, result.Position);
    }

    [Fact]
    public async Task GetNbaPlayerAsync_WithDetailedSize_ReturnsDetailedResponse()
    {
        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);
        player.NbaId = 2544;
        player.JerseyNumber = 23;

        _playerRepository
            .Setup(repository => repository.GetByIdAsync(
                playerId,
                PlayerResponseSize.Detailed,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        var result = await _playerService.GetNbaPlayerAsync(
            playerId, PlayerResponseSize.Detailed, CancellationToken.None);

        var response = Assert.IsType<NbaPlayerDetailedResponse>(result);
        Assert.Equal(player.NbaId, response.NbaId);
        Assert.Equal(player.JerseyNumber, response.JerseyNumber);
        _playerRepository.Verify(repository => repository.GetByIdAsync(
            playerId,
            PlayerResponseSize.Detailed,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNbaPlayerAsync_WithExtendedSize_MapsSeasonStats()
    {
        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);
        player.SeasonStats.Add(new PlayerStats
        {
            NbaPlayerId = playerId,
            Season = 2026,
            GamesPlayed = 10,
            PointsPerGame = 25.4,
            NbaPlayer = player
        });

        _playerRepository
            .Setup(repository => repository.GetByIdAsync(
                playerId,
                PlayerResponseSize.Extended,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        var result = await _playerService.GetNbaPlayerAsync(
            playerId, PlayerResponseSize.Extended, CancellationToken.None);

        var response = Assert.IsType<NbaPlayerExtendedResponse>(result);
        var stats = Assert.Single(response.SeasonStats);
        Assert.Equal(2026, stats.Season);
        Assert.Equal(25.4, stats.PointsPerGame);
    }

    private static NbaPlayer CreatePlayer(Guid id)
    {
        var expectedUser = new NbaPlayer
        {
            Id = id,
            FirstName = "Lebron",
            LastName = "James",
            Team = "Lakers",
            Position = "F"
        };

        return expectedUser;
    }
}
