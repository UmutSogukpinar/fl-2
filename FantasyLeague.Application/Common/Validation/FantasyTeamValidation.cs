using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Leagues;

namespace FantasyLeague.Application.Common.Validation;

internal static class FantasyTeamValidation
{
    public static void ValidateCreateFantasyTeamRequest(
        this CreateFantasyTeamRequest request
    )
    {
        ValidateName(request.Name);
        ValidateRequiredId(request.LeagueId, "LeagueId");
        ValidateRequiredId(request.OwnerId, "OwnerId");
    }

    public static void ValidateJoinLeagueRequest(this JoinLeagueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JoinCode))
        {
            throw new BadRequestException("JoinCode is required.");
        }

        ValidateName(request.TeamName);
        ValidateRequiredId(request.OwnerId, "OwnerId");
    }

    public static void ValidateUpdateFantasyTeamRequest(
        this UpdateFantasyTeamRequest request
    )
    {
        ValidateName(request.Name);
    }

    // ================== Utils ==================

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException(
                "Team name is required."
            );
        }
    }

    private static void ValidateRequiredId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
        {
            throw new BadRequestException($"{fieldName} is required.");
        }
    }

}
