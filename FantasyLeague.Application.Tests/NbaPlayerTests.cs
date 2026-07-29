using Moq;
using Xunit;

using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Caching;

namespace FantasyLeague.Application.Tests;

public class NbaPlayerTests
{
    private readonly Mock<INbaPlayerRepository> _playerRepository;
    private readonly Mock<ICacheService> _cacheService;
    private readonly NbaPlayerService _playerService;

    public NbaPlayerTests()
    {
        _playerRepository = new Mock<INbaPlayerRepository>();
        _cacheService = new Mock<ICacheService>();
        _cacheService
            .Setup(cache => cache.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IPlayerResponse>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<CancellationToken, Task<IPlayerResponse>> factory,
                TimeSpan _, CancellationToken token) => factory(token));
        _playerService = new NbaPlayerService(
            _playerRepository.Object, _cacheService.Object);
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
        var season = 2024;

        var playerId = Guid.NewGuid();
        IPlayerResponse player = new NbaPlayerBasicResponse(
            playerId, playerName, playerLastName, playerTeam, playerPosition);

        _playerRepository.Setup(repo => repo.GetByIdAndSeasonAsync(
            playerId, season, PlayerResponseSize.Basic, It.IsAny<CancellationToken>())!
        )
        .ReturnsAsync(player);

        // Act
        var result = await _playerService.GetNbaPlayerByIdAndYearAsync(
            playerId, season, PlayerResponseSize.Basic
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
        var player = new NbaPlayerDetailedResponse(
            playerId, 2544, "Lebron", "James", "Lakers", "F",
            23, null, null, DateTime.UtcNow, null);
        var season = 2024;

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
            PlayerResponseSize.Detailed

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
        var season = 2024;
        var player = new NbaPlayerExtendedResponse(
            playerId, 2544, "Lebron", "James", "Lakers", "F",
            23, null, null, DateTime.UtcNow, null,
            new PlayerStatsResponse(
                season, 10, 25.4, 0, 0, 0, 0, 0, 0, 0, 0, 0));

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
            PlayerResponseSize.Extended

        );

        // Assert
        Assert.NotNull(result);
        var response = Assert.IsType<NbaPlayerExtendedResponse>(result);
        var stats = Assert.IsType<PlayerStatsResponse>(response.SeasonStats);
        Assert.Equal(season, stats.Season);
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
        var season = 2024;

        _playerRepository
            .Setup(s => s.GetByIdAndSeasonAsync(
                        playerId,
                        season,
                        PlayerResponseSize.Basic,
                        It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPlayerResponse?)null);


        // Act & Assert

        await Assert.ThrowsAsync<NotFoundException>(
             async () => await _playerService.GetNbaPlayerByIdAndYearAsync(
                    playerId,
                    season,
                    PlayerResponseSize.Basic

                )
        );

    }

}
