using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository
{
    public async Task<MatchStats> GetMatchStatsByTeamIdsAsync(
        Guid leagueId,
        Guid homeTeamId,
        Guid awayTeamId,
        int season,
        CancellationToken cancellationToken)
    {
        var teamIds = new[] { homeTeamId, awayTeamId };

        var teamStats = await (
            from rosterPlayer in _dbContext.Set<FantasyTeamPlayer>()
                .AsNoTracking()
            join stats in _dbContext.Set<PlayerStats>().AsNoTracking()
                on rosterPlayer.NbaPlayerId equals stats.NbaPlayerId
            where rosterPlayer.LeagueId == leagueId
                && teamIds.Contains(rosterPlayer.FantasyTeamId)
                && stats.Season == season
            group stats by rosterPlayer.FantasyTeamId
            into team
            select new TeamMatchStats(
                team.Key,
                season,
                team.Count(),
                team.Sum(stats => stats.GamesPlayed),
                team.Sum(stats => stats.GamesStarted),
                team.Sum(stats => stats.MinutesPerGame),
                team.Sum(stats => stats.PointsPerGame),
                team.Sum(stats => stats.ReboundsPerGame),
                team.Sum(stats => stats.AssistsPerGame),
                team.Sum(stats => stats.StealsPerGame),
                team.Sum(stats => stats.BlocksPerGame),
                team.Sum(stats => stats.TurnoversPerGame),
                team.Average(stats => stats.FieldGoalPercentage),
                team.Average(stats => stats.ThreePointPercentage),
                team.Average(stats => stats.FreeThrowPercentage)))
            .ToArrayAsync(cancellationToken);

        var homeTeamStats = teamStats.SingleOrDefault(
            stats => stats.FantasyTeamId == homeTeamId)
            ?? TeamMatchStats.Empty(homeTeamId, season);
        var awayTeamStats = teamStats.SingleOrDefault(
            stats => stats.FantasyTeamId == awayTeamId)
            ?? TeamMatchStats.Empty(awayTeamId, season);

        return new MatchStats(homeTeamStats, awayTeamStats);
    }
}
 
