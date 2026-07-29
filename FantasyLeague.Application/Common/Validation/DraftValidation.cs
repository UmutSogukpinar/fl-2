using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Drafts;

namespace FantasyLeague.Application.Common.Validation;

internal static class DraftValidation
{
    public static void ValidateMakeDraftPickRequest(this MakeDraftPickRequest request)
    {
        if (request.TeamId == Guid.Empty)
        {
            throw new BadRequestException("TeamId is required.");
        }

        if (request.OwnerId == Guid.Empty)
        {
            throw new BadRequestException("OwnerId is required.");
        }

        if (request.NbaPlayerId == Guid.Empty)
        {
            throw new BadRequestException("NbaPlayerId is required.");
        }
    }
}
