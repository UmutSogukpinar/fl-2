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
        CancellationToken cancellation)
    {
        var teamIds = new[] { homeTeamId, awayTeamId };

        var teamStats = await (
            from rosterPlayer in _dbContext.Set<FantasyTeamPlayer>().AsNoTracking()
            join stats in _dbContext.Set<PlayerStats>().AsNoTracking()
                on rosterPlayer.NbaPlayerId equals stats.NbaPlayerId
            join league in _dbContext.Set<League>().AsNoTracking()
                on rosterPlayer.LeagueId equals league.Id
            where rosterPlayer.LeagueId == leagueId
                  && teamIds.Contains(rosterPlayer.FantasyTeamId)
                  && stats.Season == season
            group stats by new
            {
                rosterPlayer.FantasyTeamId,
                league.Settings.RosterSize
            }
            into team
            select new TeamMatchStats(
                team.Key.FantasyTeamId,
                season,
                team.Key.RosterSize,
                (int)Math.Round(team.Average(stats => stats.GamesPlayed)),
                (int)Math.Round(team.Average(stats => stats.GamesStarted)),
                team.Average(stats => stats.MinutesPerGame),
                team.Average(stats => stats.PointsPerGame),
                team.Average(stats => stats.ReboundsPerGame),
                team.Average(stats => stats.AssistsPerGame),
                team.Average(stats => stats.StealsPerGame),
                team.Average(stats => stats.BlocksPerGame),
                team.Average(stats => stats.TurnoversPerGame),
                team.Average(stats => stats.FieldGoalPercentage),
                team.Average(stats => stats.ThreePointPercentage),
                team.Average(stats => stats.FreeThrowPercentage)))
            .ToArrayAsync(cancellation);

        var homeTeamStats = teamStats.SingleOrDefault(
            stats => stats.FantasyTeamId == homeTeamId)
            ?? TeamMatchStats.Empty(homeTeamId, season);

        var awayTeamStats = teamStats.SingleOrDefault(
            stats => stats.FantasyTeamId == awayTeamId)
            ?? TeamMatchStats.Empty(awayTeamId, season);

        return new MatchStats(homeTeamStats, awayTeamStats);
    }
}
 
