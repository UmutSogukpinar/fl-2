using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.Application.Common.Normalization;

internal static class LeagueNormalization
{
    public static CreateLeagueRequest NormalizeCreateLeagueRequest(
        this CreateLeagueRequest? request)
    {
        if (request is null)
        {
            throw new BadRequestException("Request body is required.");
        }

        return request with
        {
            Name = NormalizeRequiredText(request.Name),
            Description = NormalizeOptionalText(request.Description),
            TeamName = NormalizeOptionalText(request.TeamName)
        };
    }

    public static UpdateLeagueRequest NormalizeUpdateLeagueRequest(
        this UpdateLeagueRequest? request)
    {
        if (request is null)
        {
            throw new BadRequestException("Request body is required.");
        }

        return request with
        {
            Name = NormalizeRequiredText(request.Name),
            Description = NormalizeOptionalText(request.Description)
        };
    }

    private static string NormalizeRequiredText(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
