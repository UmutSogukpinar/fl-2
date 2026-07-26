using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

namespace FantasyLeague.Application.Common.Validation;

public class FantasyTeamValidation
{
    public static void ValidateCreateUserRequest(
        CreateFantasyTeamRequest request
    )
    {
        ValidateName(request.Name);
    }

    public static void ValidateUpdateUserRequest(
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
