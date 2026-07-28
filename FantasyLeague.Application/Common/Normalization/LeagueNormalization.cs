using FantasyLeague.Application.DTOs.Requests.Leagues;

namespace FantasyLeague.Application.Common.Normalization;

internal static class LeagueNormalization
{
    public static void NormalizeCreateLeagueRequest(
        ref CreateLeagueRequest request)
    {
        request = request with
        {
            Name = NormalizeRequiredText(request.Name),
            Description = NormalizeOptionalText(request.Description),
            TeamName = NormalizeOptionalText(request.TeamName)
        };
    }

    public static void NormalizeUpdateLeagueRequest(
        ref UpdateLeagueRequest request)
    {
        request = request with
        {
            Name = NormalizeRequiredText(request.Name),
            Description = NormalizeOptionalText(request.Description)
        };
    }

    private static string NormalizeRequiredText(string value) => value.Trim();

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
