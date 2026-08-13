using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

using FantasyLeague.Application.DTOs.Requests.Leagues;

namespace FantasyLeague.Application.Common.Normalization;

internal static class FantasyTeamNormalization
{
    public static CreateFantasyTeamRequest NormalizeCreateFantasyTeamRequest(
        this CreateFantasyTeamRequest? req
    )
    {
        if (req is null)
            throw new BadRequestException(
                "Request body is required."
            );

        return req with
        {
            Name = NormalizeName(req.Name),
        };
    }

    public static UpdateFantasyTeamRequest NormalizeUpdateFantasyTeamRequest(
        this UpdateFantasyTeamRequest? req
    )
    {
        if (req is null)
            throw new BadRequestException(
                "Request body is required."
            );

        return req with
        {
            Name = NormalizeName(req.Name),
        };
    }

    public static JoinLeagueRequest NormalizeJoinLeagueRequest(
        this JoinLeagueRequest? req)
    {
        if (req is null)
            throw new BadRequestException(
                "Request body is required."
            );

        return req with
        {
            JoinCode = req.JoinCode?.Trim().ToUpperInvariant() ?? string.Empty,
            TeamName = NormalizeName(req.TeamName)
        };
    }

    // ================== Utils ==================

    private static string NormalizeName(string? name)
    {
        return name?.Trim().ToLowerInvariant()!;
    }
}
