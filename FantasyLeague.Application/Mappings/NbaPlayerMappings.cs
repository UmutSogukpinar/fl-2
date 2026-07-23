using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Mappings;

public static class NbaPlayerMappings
{
    public static IPlayerResponse Map(
    NbaPlayer player,
    PlayerResponseSize size)
    {
        return size switch
        {
            PlayerResponseSize.Basic =>
                ToBasicResponse(player),

            PlayerResponseSize.Detailed =>
                ToDetailedResponse(player),

            PlayerResponseSize.Extended =>
                ToExtendedResponse(player),

            _ => throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                "Invalid player response size.")
        };
    }

    public static NbaPlayerBasicResponse ToBasicResponse(NbaPlayer player)
    {
        return new NbaPlayerBasicResponse(
            player.Id,
            player.FirstName,
            player.LastName,
            player.Team ?? "Unknown",
            player.Position ?? "Unknown"
        );
    }

    public static NbaPlayerDetailedResponse ToDetailedResponse(NbaPlayer player)
    {
        return new NbaPlayerDetailedResponse(
            player.Id,
            player.NbaId,
            player.FirstName,
            player.LastName,
            player.Team ?? "Unknown",
            player.Position ?? "Unknown",
            player.JerseyNumber,
            player.HeightCm,
            player.WeightKg,
            player.CreatedAt,
            player.UpdatedAt
        );
    }

    public static NbaPlayerExtendedResponse ToExtendedResponse(
        NbaPlayer player
    )
    {
        var seasonStatsResponses = player.SeasonStats.Select(
            stat => new PlayerStatsResponse(
                stat.Season,
                stat.GamesPlayed,
                stat.PointsPerGame,
                stat.AssistsPerGame,
                stat.ReboundsPerGame,
                stat.StealsPerGame,
                stat.BlocksPerGame,
                stat.TurnoversPerGame,
                stat.FieldGoalPercentage,
                stat.ThreePointPercentage,
                stat.FreeThrowPercentage,
                stat.MinutesPerGame
            ));

        return new NbaPlayerExtendedResponse(
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
            seasonStatsResponses
        );
    }
}
