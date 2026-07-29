using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Validation;

internal static class PaginationValidation
{
    public static void ValidatePaginationRequest(this PaginationRequest request)
    {
        if (request.PageNumber < 1)
        {
            throw new BadRequestException("PageNumber must be at least 1.");
        }

        if (request.PageSize is < 1 or > 100)
        {
            throw new BadRequestException("PageSize must be between 1 and 100.");
        }
    }
}
