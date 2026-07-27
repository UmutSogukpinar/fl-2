using System.Globalization;
using FantasyLeague.Application.Common.Interfaces.ExternalServices;

namespace FantasyLeague.Infrastructure.ExternalServices.NbaApi;

internal static class ApiSportsMapper
{
    public static bool IsActivePlayer(ApiPlayer player) =>
        player.Leagues?.Standard?.Active == true;

    public static ExternalNbaPlayer ToExternalPlayer(ApiPlayer player, string? team) => new(
        player.Id,
        player.FirstName,
        player.LastName,
        team,
        player.Leagues?.Standard?.Position,
        player.Leagues?.Standard?.JerseyNumber,
        ToCentimeters(player.Height?.Meters),
        ParseNullableDecimal(player.Weight?.Kilograms));

    public static ExternalPlayerGameStats ToExternalStats(ApiPlayerStats stats) => new(
        stats.Player.Id,
        stats.Game.Id,
        stats.Team?.Code,
        stats.Position,
        ParseMinutes(stats.Minutes),
        stats.Points ?? 0,
        stats.TotalRebounds ?? 0,
        stats.Assists ?? 0,
        stats.Steals ?? 0,
        stats.Blocks ?? 0,
        stats.Turnovers ?? 0,
        stats.FieldGoalsMade ?? 0,
        stats.FieldGoalsAttempted ?? 0,
        stats.ThreePointersMade ?? 0,
        stats.ThreePointersAttempted ?? 0,
        stats.FreeThrowsMade ?? 0,
        stats.FreeThrowsAttempted ?? 0);

    private static int? ToCentimeters(string? meters)
    {
        var value = ParseNullableDecimal(meters);
        return value.HasValue ? (int)Math.Round(value.Value * 100) : null;
    }

    private static decimal? ParseNullableDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;

    private static decimal ParseMinutes(string? minutes)
    {
        if (string.IsNullOrWhiteSpace(minutes)) return 0;

        var parts = minutes.Split(':');
        if (!decimal.TryParse(
                parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return 0;

        if (parts.Length == 2
            && decimal.TryParse(
                parts[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var seconds))
            value += seconds / 60;

        return value;
    }
}
