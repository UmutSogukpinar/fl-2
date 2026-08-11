using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.Common.Interfaces.ExternalServices;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Domain.Entities;
using Moq;

namespace FantasyLeague.Application.Tests.Services.NbaPlayers;

public sealed class NbaPlayerSyncServiceTests
{
    private const int Season = 2024;
    private readonly Mock<INbaPlayersApiClient> _apiClient = new();
    private readonly Mock<INbaPlayerRepository> _playerRepository = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly NbaPlayerSyncService _service;

    public NbaPlayerSyncServiceTests()
    {
        _service = new NbaPlayerSyncService(
            _apiClient.Object,
            _playerRepository.Object,
            _cacheService.Object);
    }

    // Case: Sync Active Players
    // Reasoning: This test verifies the Sync Active Players operation.
    // Expected Result: The expected outcome is: Creates Player And Aggregated Statistics.
    [Fact]
    public async Task SyncActivePlayersAsync_CreatesPlayerAndAggregatedStatistics()
    {
        var sourcePlayer = new ExternalNbaPlayer(
            23, "LeBron", "James", null, "F", 23, 206, 113m);
        ExternalPlayerGameStats[] sourceStats =
        [
            CreateStats(23, gameId: 1, team: "LAL", minutes: 30, points: 20,
                rebounds: 8, fieldGoalsMade: 8, fieldGoalsAttempted: 16),
            CreateStats(23, gameId: 2, team: "LAL", minutes: 20, points: 10,
                rebounds: 4, fieldGoalsMade: 4, fieldGoalsAttempted: 10),
            // Aynı maç ikinci kez gelirse yalnızca ilk kayıt hesaplanmalı.
            CreateStats(23, gameId: 2, team: "LAL", minutes: 20, points: 99),
            // Süre almayan maç, oynanan maç ve ortalama hesabına katılmamalı.
            CreateStats(23, gameId: 3, team: "BOS", minutes: 0, points: 50)
        ];

        SetupApi([sourcePlayer], sourceStats);
        _playerRepository
            .Setup(repository => repository.GetByNbaIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, NbaPlayer>());
        _playerRepository
            .Setup(repository => repository.GetPlayerStatsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                Season,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PlayerStats>());

        NbaPlayer[] addedPlayers = [];
        PlayerStats[] addedStats = [];
        _playerRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IEnumerable<NbaPlayer>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NbaPlayer>, CancellationToken>(
                (players, _) => addedPlayers = players.ToArray())
            .Returns(Task.CompletedTask);
        _playerRepository
            .Setup(repository => repository.AddStatsRangeAsync(
                It.IsAny<IEnumerable<PlayerStats>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<PlayerStats>, CancellationToken>(
                (stats, _) => addedStats = stats.ToArray())
            .Returns(Task.CompletedTask);

        var response = await _service.SyncActivePlayersAsync();

        var player = Assert.Single(addedPlayers);
        Assert.Equal("LAL", player.Team);
        Assert.Equal(1, response.ProcessedCount);
        Assert.Equal(1, response.CreatedCount);
        Assert.Equal(0, response.UpdatedCount);
        Assert.Equal(1, response.StatisticsProcessedCount);

        var aggregate = Assert.Single(addedStats);
        Assert.Equal(2, aggregate.GamesPlayed);
        Assert.Equal(15, aggregate.PointsPerGame);
        Assert.Equal(6, aggregate.ReboundsPerGame);
        Assert.Equal(46.15, aggregate.FieldGoalPercentage);
        Assert.Equal(player.Id, aggregate.NbaPlayerId);
        _playerRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(
            cache => cache.Remove(It.IsAny<string>()),
            Times.Exactly(3));
    }

    // Case: Sync Active Players
    // Reasoning: This test verifies the Sync Active Players operation.
    // Expected Result: The expected outcome is: Updates Existing Player And Statistics.
    [Fact]
    public async Task SyncActivePlayersAsync_UpdatesExistingPlayerAndStatistics()
    {
        var existingPlayer = new NbaPlayer
        {
            Id = Guid.NewGuid(),
            NbaId = 30,
            FirstName = "Old",
            LastName = "Name",
            Team = "OLD"
        };
        var existingStats = new PlayerStats
        {
            NbaPlayerId = existingPlayer.Id,
            Season = Season,
            GamesPlayed = 1,
            PointsPerGame = 1
        };
        var sourcePlayer = new ExternalNbaPlayer(
            30, "Stephen", "Curry", "GSW", "G", 30, 188, 84m);
        var sourceStats = new[]
        {
            CreateStats(30, 10, "GSW", 36, 32, assists: 9,
                threePointersMade: 6, threePointersAttempted: 12)
        };

        SetupApi([sourcePlayer], sourceStats);
        _playerRepository
            .Setup(repository => repository.GetByNbaIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, NbaPlayer> { [30] = existingPlayer });
        _playerRepository
            .Setup(repository => repository.GetPlayerStatsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                Season,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PlayerStats>
            {
                [existingPlayer.Id] = existingStats
            });

        var response = await _service.SyncActivePlayersAsync();

        Assert.Equal(0, response.CreatedCount);
        Assert.Equal(1, response.UpdatedCount);
        Assert.Equal("Stephen", existingPlayer.FirstName);
        Assert.Equal("Curry", existingPlayer.LastName);
        Assert.Equal("GSW", existingPlayer.Team);
        Assert.NotNull(existingPlayer.UpdatedAt);
        Assert.Equal(1, existingStats.GamesPlayed);
        Assert.Equal(32, existingStats.PointsPerGame);
        Assert.Equal(9, existingStats.AssistsPerGame);
        Assert.Equal(50, existingStats.ThreePointPercentage);
        Assert.NotNull(existingStats.UpdatedAt);
        _playerRepository.Verify(repository => repository.AddRangeAsync(
            It.Is<IEnumerable<NbaPlayer>>(players => !players.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
        _playerRepository.Verify(repository => repository.AddStatsRangeAsync(
            It.Is<IEnumerable<PlayerStats>>(stats => !stats.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Sync Active Players
    // Reasoning: This test verifies the Sync Active Players operation.
    // Expected Result: The expected outcome is: Ignores Statistics For Unknown Players.
    [Fact]
    public async Task SyncActivePlayersAsync_IgnoresStatisticsForUnknownPlayers()
    {
        SetupApi([], [CreateStats(999, 1, "UNK", 20, 10)]);
        _playerRepository
            .Setup(repository => repository.GetByNbaIdsAsync(
                It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, NbaPlayer>());
        _playerRepository
            .Setup(repository => repository.GetPlayerStatsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                Season,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, PlayerStats>());

        var response = await _service.SyncActivePlayersAsync();

        Assert.Equal(0, response.ProcessedCount);
        Assert.Equal(0, response.StatisticsProcessedCount);
        _playerRepository.Verify(repository => repository.AddStatsRangeAsync(
            It.Is<IEnumerable<PlayerStats>>(stats => !stats.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupApi(
        IReadOnlyCollection<ExternalNbaPlayer> players,
        IReadOnlyCollection<ExternalPlayerGameStats> stats)
    {
        _apiClient
            .Setup(client => client.GetActivePlayersAsync(
                Season, It.IsAny<CancellationToken>()))
            .ReturnsAsync(players);
        _apiClient
            .Setup(client => client.GetPlayerStatisticsAsync(
                Season, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);
    }

    private static ExternalPlayerGameStats CreateStats(
        int nbaPlayerId,
        int gameId,
        string team,
        decimal minutes,
        int points,
        int rebounds = 0,
        int assists = 0,
        int fieldGoalsMade = 0,
        int fieldGoalsAttempted = 0,
        int threePointersMade = 0,
        int threePointersAttempted = 0) => new(
            nbaPlayerId,
            gameId,
            team,
            "G",
            minutes,
            points,
            rebounds,
            assists,
            0,
            0,
            0,
            fieldGoalsMade,
            fieldGoalsAttempted,
            threePointersMade,
            threePointersAttempted,
            0,
            0);
}
