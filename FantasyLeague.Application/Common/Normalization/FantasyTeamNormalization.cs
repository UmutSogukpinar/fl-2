using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

using FantasyLeague.Application.DTOs.Requests.Leagues;

namespace FantasyLeague.Application.Common.Normalization;

internal static class FantasyTeamNormalization
{
    public static void NormalizeCreateFantasyTeamRequest(
        ref CreateFantasyTeamRequest request
    )
    {
        request = request with
        {
            Name = NormalizeName(request.Name),
        };
    }

    public static void NormalizeUpdateFantasyTeamRequest(
        ref UpdateFantasyTeamRequest request
    )
    {
        request = request with
        {
            Name = NormalizeName(request.Name),
        };
    }

    public static void NormalizeJoinLeagueRequest(
        ref JoinLeagueRequest request)
    {
        request = request with
        {
            JoinCode = request.JoinCode.Trim().ToUpperInvariant(),
            TeamName = NormalizeName(request.TeamName)
        };
    }

    // ================== Utils ==================

    private static string NormalizeName(string? name)
    {
        return name?.Trim().ToLowerInvariant()!;
    }
}
