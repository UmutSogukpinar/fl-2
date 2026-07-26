using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

namespace FantasyLeague.Application.Common.Normalization;

public class FantasyTeamNormalization
{
    public static void NormalizeCreateUserRequest(
        ref CreateFantasyTeamRequest request
    )
    {
        request = request with
        {
            Name = NormalizeName(request.Name),
        };
    }

    public static void NormalizeUpdateUserRequest(
        ref UpdateFantasyTeamRequest request
    )
    {
        request = request with
        {
            Name = NormalizeName(request.Name),
        };
    }

    // ================== Utils ==================

    private static string NormalizeName(string? name)
    {
        return name?.ToLower()!;
    }
}
