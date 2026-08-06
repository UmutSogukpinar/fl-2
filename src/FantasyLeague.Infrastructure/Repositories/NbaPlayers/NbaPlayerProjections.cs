using FantasyLeague.Domain.Entities.Players;

using System.Linq.Expressions;

using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

internal static class NbaPlayerProjections
{
    internal static readonly Expression<Func<NbaPlayer, NbaPlayerBasicResponse>>
    Basic =
        player => new NbaPlayerBasicResponse(
            player.Id,
            player.FirstName,
            player.LastName,
            player.Team ?? "Unknown",
            player.Position ?? "Unknown"
        );

    internal static readonly Expression<Func<NbaPlayer, NbaPlayerDetailedResponse>>
    Detailed =
        player => new NbaPlayerDetailedResponse(
            player.Id,
            player.NbaId,
            player.FirstName,
            player.LastName,
            player.Team,
            player.Position,
            player.JerseyNumber,
            player.HeightCm,
            player.WeightKg,
            player.CreatedAt,
            player.UpdatedAt
        );

    internal static Expression<Func<NbaPlayer, NbaPlayerExtendedResponse>>
    Extended(int season)
    {
        return player => new NbaPlayerExtendedResponse(
            player.Id,
            player.NbaId,
            player.FirstName,
            player.LastName,
            player.Team,
            player.Position,
            player.JerseyNumber,
            player.HeightCm,
            player.WeightKg,
            player.CreatedAt,
            player.UpdatedAt,
            player.SeasonStats
                .Where(stats => stats.Season == season)
                .Select(stats => new PlayerStatsResponse(
                    stats.Season,
                    stats.GamesPlayed,
                    stats.PointsPerGame,
                    stats.ReboundsPerGame,
                    stats.AssistsPerGame,
                    stats.StealsPerGame,
                    stats.BlocksPerGame,
                    stats.TurnoversPerGame,
                    stats.FieldGoalPercentage,
                    stats.ThreePointPercentage,
                    stats.FreeThrowPercentage,
                    stats.MinutesPerGame))
                .SingleOrDefault()
        );
    }

    internal static IQueryable<NbaPlayerBasicResponse> ToBasic(
        this IQueryable<NbaPlayer> query)
    {
        return query.Select(Basic);
    }

    internal static IQueryable<NbaPlayerDetailedResponse> ToDetailed(
        this IQueryable<NbaPlayer> query)
    {
        return query.Select(Detailed);
    }

    internal static IQueryable<NbaPlayerExtendedResponse> ToExtended(
        this IQueryable<NbaPlayer> query,
        int season)
    {
        return query.Select(Extended(season));
    }
}
