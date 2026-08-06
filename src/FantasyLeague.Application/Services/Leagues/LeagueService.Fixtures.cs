using FantasyLeague.Domain.Entities.Leagues;

namespace FantasyLeague.Application.Services.Leagues;

using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using Microsoft.Extensions.Logging;

public sealed partial class LeagueService
{
    public async Task<int> ProcessDueFixturesAsync(
        DateTime utcNow,
        CancellationToken cancellation = default
    )
    {
        var fixtures = await _leagueSetupRepository
                            .GetDueFixturesAsync(utcNow, cancellation);
        var processedFixtures = new List<LeagueFixture>();

        foreach (var fixture in fixtures)
        {
            var league = await _leagueRepository
                                .GetResponseByIdAsync(
                                    fixture.LeagueId, cancellation
                                );

            if (league is null)
                continue;

            var stats = await _playerRepository.GetMatchStatsByTeamIdsAsync(
                fixture.LeagueId,
                fixture.HomeTeamId,
                fixture.AwayTeamId,
                league.Season,
                cancellation
            );

            if (stats.HomeTeamStats.PlayerCount == 0 ||
                stats.AwayTeamStats.PlayerCount == 0 ||
                stats.HomeTeamStats.GamesPlayed == 0 ||
                stats.AwayTeamStats.GamesPlayed == 0)
            {
                _logger.LogWarning(
                    "Fixture {FixtureId} was not scored because player stats " +
                    "are missing. League: {LeagueId}, Season: {Season}, " +
                    "Home players/games: {HomePlayerCount}/{HomeGamesPlayed}, " +
                    "Away players/games: {AwayPlayerCount}/{AwayGamesPlayed}",
                    fixture.Id,
                    fixture.LeagueId,
                    league.Season,
                    stats.HomeTeamStats.PlayerCount,
                    stats.HomeTeamStats.GamesPlayed,
                    stats.AwayTeamStats.PlayerCount,
                    stats.AwayTeamStats.GamesPlayed
                );
                continue;
            }

            fixture.Status = MatchStatus.InProgress;

            fixture.HomeScore = Score(stats.HomeTeamStats);
            fixture.AwayScore = Score(stats.AwayTeamStats);

            fixture.Status = MatchStatus.Completed;
            processedFixtures.Add(fixture);
        }

        await CommitProcessAsync(processedFixtures, utcNow, cancellation);

        return processedFixtures.Count;
    }

    private async Task CommitProcessAsync(
        IReadOnlyList<LeagueFixture> fixtures,
        DateTime utcNow,
        CancellationToken cancellation = default
    )
    {
        if (fixtures.Count > 0)
        {
            await _leagueSetupRepository.SaveChangesAsync(cancellation);

            foreach (
                var leagueId in fixtures
                                .Select(fixture => fixture.LeagueId)
                                .Distinct()
            )
            {
                if (await _leagueSetupRepository
                    .HasUnfinishedFixturesAsync(
                        leagueId, cancellation))
                {
                    continue;
                }

                var league = await _leagueRepository
                                   .GetTrackedByIdAsync(
                                        leagueId, cancellation
                                   );

                if (league is null ||
                    league.Status == LeagueStatus.Completed
                )
                    continue;

                league.Status = LeagueStatus.Completed;
                league.UpdatedAt = utcNow;

                await _leagueRepository.SaveChangesAsync(cancellation);
            }
        }
    }

    private int Score(TeamMatchStats stats)
    {
        double densityCoef;
        try
        {
            densityCoef = CalculateDensityCoefficient(stats.GamesPlayed);
            densityCoef += CalculateEfficiencyCoefficient(stats);
            densityCoef -= CalculateInefficiencyCoefficient(stats);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to calculate the density coefficient." +
                "Games played: {GamesPlayed}",
                stats.GamesPlayed
            );

            densityCoef = 0.0;
        }

        var result = CalculateOffenseValue(stats) +
                    CalculateDefensiveValue(stats);

        return (int)Math.Round(result * densityCoef);
    }

    private static double CalculateOffenseValue(
        TeamMatchStats stats
    )
    {
        var points = stats.PointsPerGame * 5;
        var assists = stats.AssistsPerGame * 7;
        var rebound = stats.ReboundsPerGame * 7;

        return (points + assists + rebound);
    }

    private static double CalculateDefensiveValue(
        TeamMatchStats stats
    )
    {
        return (25 * (stats.StealsPerGame + stats.BlocksPerGame));
    }

    private static double CalculateInefficiencyCoefficient(
        TeamMatchStats stats
    )
    {
        var turnover = stats.TurnoversPerGame;

        return turnover switch
        {
            < 0 => throw new ArgumentOutOfRangeException(
                nameof(turnover),
                turnover,
                "The 'Turnover' stats cannot be under 0"
            ),

            < 1 => 0.1,
            < 2 => 0.2,
            < 4 => 0.5,
            < 6 => 0.7,
            _ => 1
        };
    }

    private static double CalculateEfficiencyCoefficient(
        TeamMatchStats stats
    )
    {
        var gamePercantages = new[]
        {
            (percantage: stats.ThreePointPercentage, weight: 0.5),
            (percantage: stats.FieldGoalPercentage, weight: 0.35),
            (percantage: stats.FreeThrowPercentage, weight: 0.15)
        };
        var sum = gamePercantages.Sum(per => per.percantage * per.weight);
        var amount = gamePercantages.Sum(per => per.weight) * 10;

        return (sum / amount);
    }

    private static double CalculateDensityCoefficient(int gamesPlayed) =>
    gamesPlayed switch
    {
        < 0 or > 82 => throw new ArgumentOutOfRangeException(
            nameof(gamesPlayed),
            gamesPlayed,
            "Games played must be between 0 and 82."
        ),

        < 21 => 1.00,
        < 42 => 1.05,
        < 63 => 1.10,
        _ => 1.15
    };
}
