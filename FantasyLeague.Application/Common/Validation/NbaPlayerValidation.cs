using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Validation;

internal static class NbaPlayerValidation
{
    public static void ValidateNbaPlayerRequest(
        GetNbaPlayersRequest req
    )
    {
        ValidateExistenceOfKeyAttr(req);
    }

    // ================== Utils ==================

    private static void ValidateExistenceOfKeyAttr(
        GetNbaPlayersRequest req
    )
    {
        if (req is null)
            throw new BadRequestException(
                "GetNbaPlayerRequest Object is null!"
            );

        if (req.Id == default
            && string.IsNullOrEmpty(req.Name)
            && string.IsNullOrEmpty(req.Surname)
        )
            throw new BadRequestException(
                "At least one of 'id', 'name'," +
                "or 'surname' must be provided."
            );
    }
}
