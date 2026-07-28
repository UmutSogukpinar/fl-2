using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

namespace FantasyLeague.Application.Common.Validation;

internal static class FantasyTeamValidation
{
    public static void ValidateCreateFantasyTeamRequest(
        CreateFantasyTeamRequest request
    )
    {
        ValidateName(request.Name);
    }

    public static void ValidateUpdateFantasyTeamRequest(
        UpdateFantasyTeamRequest request
    )
    {
        ValidateName(request.Name);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException(
                "Team name is required."
            );
        }
    }

}
