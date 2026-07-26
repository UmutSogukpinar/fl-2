using Moq;
using Xunit;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;

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
    public async Task DoesFindBasicNbaPlayerById()
    {
        // Arrange
        var playerName = "Lebron";
        var playerLastName = "James";
        var playerTeam = "Lakers";
        var playerPosition = "F";
        var season = 2025;

        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);

        _playerRepository.Setup(repo => repo.GetByIdAndSeasonAsync(
            playerId, season, PlayerResponseSize.Basic, It.IsAny<CancellationToken>())!
        )
        .ReturnsAsync(player);

        // Act
        var result = await _playerService.GetNbaPlayerByIdAndYearAsync(
            playerId, season, PlayerResponseSize.Basic, CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(playerId, result.Id);
        Assert.Equal(playerName, result.FirstName);
        Assert.Equal(playerLastName, result.LastName);
        Assert.Equal(playerTeam, result.Team);
        Assert.Equal(playerPosition, result.Position);
    }


    // Case: Nba player with Detailed size
    // Reasoning: The service should return
    // detailed player information
    // Expected Result: The returned player information
    [Fact]
    public async Task DoesFindDetailedUserById()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);
        player.NbaId = 2544;
        player.JerseyNumber = 23;
        var season = 2025;

        _playerRepository
            .Setup(repository => repository.GetByIdAndSeasonAsync(
                playerId,
                season,
                PlayerResponseSize.Detailed,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        // Act
        var result = await _playerService.GetNbaPlayerByIdAndYearAsync(
            playerId,
            season,
            PlayerResponseSize.Detailed,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        var response = Assert.IsType<NbaPlayerDetailedResponse>(result);
        Assert.Equal(player.NbaId, response.NbaId);
        Assert.Equal(player.JerseyNumber, response.JerseyNumber);
        _playerRepository.Verify(repository => repository.GetByIdAndSeasonAsync(
            playerId,
            season,
            PlayerResponseSize.Detailed,
            It.IsAny<CancellationToken>()), Times.Once
         );
    }

    // Case: The Nba Player with extended size
    // Reasoning: The service should return
    // detailed player information
    // Expected Result: The returned player information
    [Fact]
    public async Task DoesFindExtendedUserById()
    {
        // Assert
        var playerId = Guid.NewGuid();
        var player = CreatePlayer(playerId);
        var season = 2025;

        player.SeasonStats.Add(new PlayerStats
        {
            NbaPlayerId = playerId,
            Season = season,
            GamesPlayed = 10,
            PointsPerGame = 25.4,
            NbaPlayer = player
        });

        _playerRepository
            .Setup(repository => repository.GetByIdAndSeasonAsync(
                playerId,
                season,
                PlayerResponseSize.Extended,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);

        // Act
        var result = await _playerService.GetNbaPlayerByIdAndYearAsync(
            playerId,
            season,
            PlayerResponseSize.Extended,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        var response = Assert.IsType<NbaPlayerExtendedResponse>(result);
        var stats = Assert.Single(response.SeasonStats);
        Assert.Equal(2026, stats.Season);
        Assert.Equal(25.4, stats.PointsPerGame);
    }

    // Case: The Nba Player not found by ID
    // Reasoning: When the player is not found by Id,
    // the service should throw a NotFoundException
    [Fact]
    public async Task DoesNotFindUserById()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var season = 2025;

        _playerRepository
            .Setup(s => s.GetByIdAndSeasonAsync(
                        playerId,
                        season,
                        PlayerResponseSize.Basic,
                        It.IsAny<CancellationToken>()))
            .ReturnsAsync((NbaPlayer?)null);


        // Act & Assert

        await Assert.ThrowsAsync<NotFoundException>(
             async () => await _playerService.GetNbaPlayerByIdAndYearAsync(
                    playerId,
                    season,
                    PlayerResponseSize.Basic,
                    CancellationToken.None
                )
        );

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
