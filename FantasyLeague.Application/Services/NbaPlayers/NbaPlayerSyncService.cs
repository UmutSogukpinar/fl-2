using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.NbaPlayers;

public sealed class NbaPlayerSyncService(
    INbaPlayersApiClient apiClient,
    INbaPlayerRepository playerRepository) : INbaPlayerSyncService
{
    private const int Season = 2024;

    public async Task<NbaPlayerSyncResponse> SyncActivePlayersAsync(
        CancellationToken cancellationToken = default)
    {
        var sourcePlayers = await apiClient.GetActivePlayersAsync(Season, cancellationToken);
        var gameStats = await apiClient.GetPlayerStatisticsAsync(Season, cancellationToken);
        sourcePlayers = AddTeamInformation(sourcePlayers, gameStats);

        var playerResult = await UpsertPlayersAsync(sourcePlayers, cancellationToken);
        var statisticsCount = await UpsertStatisticsAsync(
            playerResult.PlayersByNbaId,
            gameStats,
            cancellationToken);

        await playerRepository.SaveChangesAsync(cancellationToken);

        return new NbaPlayerSyncResponse(
            Season,
            sourcePlayers.Count,
            playerResult.CreatedCount,
            playerResult.UpdatedCount,
            statisticsCount);
    }

    private static IReadOnlyCollection<ExternalNbaPlayer> AddTeamInformation(
        IReadOnlyCollection<ExternalNbaPlayer> players,
        IReadOnlyCollection<ExternalPlayerGameStats> gameStats)
    {
        var teamsByPlayerId = gameStats
            .Where(stats => !string.IsNullOrWhiteSpace(stats.Team))
            .GroupBy(stats => stats.NbaPlayerId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(stats => stats.Team)
                    .MaxBy(team => team.Count())!
                    .Key);

        return players
            .Select(player => teamsByPlayerId.TryGetValue(player.NbaId, out var team)
                ? player with { Team = team }
                : player)
            .ToArray();
    }

    private async Task<PlayerUpsertResult> UpsertPlayersAsync(
        IReadOnlyCollection<ExternalNbaPlayer> sourcePlayers,
        CancellationToken cancellationToken)
    {
        var nbaIds = sourcePlayers.Select(player => player.NbaId).ToArray();
        var existingPlayers = await playerRepository.GetByNbaIdsAsync(nbaIds, cancellationToken);
        var playersByNbaId = existingPlayers.ToDictionary();
        var newPlayers = new List<NbaPlayer>();
        var updatedCount = 0;

        foreach (var source in sourcePlayers)
        {
            if (playersByNbaId.TryGetValue(source.NbaId, out var existingPlayer))
            {
                UpdatePlayer(existingPlayer, source);
                updatedCount++;
                continue;
            }

            var newPlayer = CreatePlayer(source);
            newPlayers.Add(newPlayer);
            playersByNbaId.Add(source.NbaId, newPlayer);
        }

        await playerRepository.AddRangeAsync(newPlayers, cancellationToken);

        return new PlayerUpsertResult(
            playersByNbaId,
            newPlayers.Count,
            updatedCount);
    }

    private async Task<int> UpsertStatisticsAsync(
        IReadOnlyDictionary<int, NbaPlayer> playersByNbaId,
        IReadOnlyCollection<ExternalPlayerGameStats> gameStats,
        CancellationToken cancellationToken)
    {
        var groupedStats = gameStats
            .Where(stats => playersByNbaId.ContainsKey(stats.NbaPlayerId))
            .GroupBy(stats => stats.NbaPlayerId)
            .ToArray();

        var playerIds = groupedStats
            .Select(group => playersByNbaId[group.Key].Id)
            .ToArray();
        var existingStats = await playerRepository.GetPlayerStatsAsync(
            playerIds,
            Season,
            cancellationToken);
        var newStats = new List<PlayerStats>();

        foreach (var group in groupedStats)
        {
            var player = playersByNbaId[group.Key];
            var aggregate = AggregateStats(player, group);

            if (existingStats.TryGetValue(player.Id, out var currentStats))
            {
                UpdateStats(currentStats, aggregate);
                continue;
            }

            newStats.Add(aggregate);
        }

        await playerRepository.AddStatsRangeAsync(newStats, cancellationToken);
        return groupedStats.Length;
    }

    private static PlayerStats AggregateStats(
        NbaPlayer player,
        IEnumerable<ExternalPlayerGameStats> source)
    {
        var games = source
            .Where(stats => stats.Minutes > 0)
            .GroupBy(stats => stats.GameId)
            .Select(group => group.First())
            .ToArray();
        var gamesPlayed = games.Length;

        return new PlayerStats
        {
            NbaPlayerId = player.Id,
            Season = Season,
            GamesPlayed = gamesPlayed,
            GamesStarted = 0,
            MinutesPerGame = Average(games, game => game.Minutes),
            PointsPerGame = Average(games, game => game.Points),
            ReboundsPerGame = Average(games, game => game.Rebounds),
            AssistsPerGame = Average(games, game => game.Assists),
            StealsPerGame = Average(games, game => game.Steals),
            BlocksPerGame = Average(games, game => game.Blocks),
            TurnoversPerGame = Average(games, game => game.Turnovers),
            FieldGoalPercentage = Percentage(
                games.Sum(game => game.FieldGoalsMade),
                games.Sum(game => game.FieldGoalsAttempted)),
            ThreePointPercentage = Percentage(
                games.Sum(game => game.ThreePointersMade),
                games.Sum(game => game.ThreePointersAttempted)),
            FreeThrowPercentage = Percentage(
                games.Sum(game => game.FreeThrowsMade),
                games.Sum(game => game.FreeThrowsAttempted)),
            NbaPlayer = player
        };
    }

    private static double Average<T>(IReadOnlyCollection<T> values, Func<T, decimal> selector)
    {
        return values.Count == 0
            ? 0
            : decimal.ToDouble(Math.Round(values.Average(selector), 2));
    }

    private static double Percentage(int made, int attempted)
    {
        return attempted == 0 ? 0 : Math.Round(made * 100d / attempted, 2);
    }

    private static NbaPlayer CreatePlayer(ExternalNbaPlayer source) => new()
    {
        NbaId = source.NbaId,
        FirstName = source.FirstName,
        LastName = source.LastName,
        Team = source.Team,
        Position = source.Position,
        JerseyNumber = source.JerseyNumber,
        HeightCm = source.HeightCm,
        WeightKg = source.WeightKg
    };

    private static void UpdatePlayer(NbaPlayer target, ExternalNbaPlayer source)
    {
        target.FirstName = source.FirstName;
        target.LastName = source.LastName;
        target.Team = source.Team;
        target.Position = source.Position;
        target.JerseyNumber = source.JerseyNumber;
        target.HeightCm = source.HeightCm;
        target.WeightKg = source.WeightKg;
        target.UpdatedAt = DateTime.UtcNow;
    }

    private static void UpdateStats(PlayerStats target, PlayerStats source)
    {
        target.GamesPlayed = source.GamesPlayed;
        target.GamesStarted = source.GamesStarted;
        target.MinutesPerGame = source.MinutesPerGame;
        target.PointsPerGame = source.PointsPerGame;
        target.ReboundsPerGame = source.ReboundsPerGame;
        target.AssistsPerGame = source.AssistsPerGame;
        target.StealsPerGame = source.StealsPerGame;
        target.BlocksPerGame = source.BlocksPerGame;
        target.TurnoversPerGame = source.TurnoversPerGame;
        target.FieldGoalPercentage = source.FieldGoalPercentage;
        target.ThreePointPercentage = source.ThreePointPercentage;
        target.FreeThrowPercentage = source.FreeThrowPercentage;
        target.UpdatedAt = DateTime.UtcNow;
    }

    private sealed record PlayerUpsertResult(
        IReadOnlyDictionary<int, NbaPlayer> PlayersByNbaId,
        int CreatedCount,
        int UpdatedCount);
}
