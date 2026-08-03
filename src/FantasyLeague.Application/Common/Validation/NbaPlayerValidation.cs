using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyLeague.Application.Common.Validation;

internal static class NbaPlayerValidation
{
    public static void ValidateNbaPlayerRequest(
        this GetNbaPlayersRequest req
    )
    {
        if (req is null)
            throw new BadRequestException(
                "GetNbaPlayerRequest Object is null!"
            );

        ValidateExistenceOfKeyAttr(req);
        ValidateSeason(req.Season);
        ValidateResponseSize(req.Size);
    }

    public static void ValidatePlayerDetailsRequest(
        this Guid id,
        int season,
        PlayerResponseSize size)
    {
        if (id == Guid.Empty)
        {
            throw new BadRequestException("Id is required.");
        }

        ValidateSeason(season);
        ValidateResponseSize(size);
    }

    // ================== Utils ==================

    private static void ValidateExistenceOfKeyAttr(
        GetNbaPlayersRequest req
    )
    {
        if (req.Id == default
            && string.IsNullOrEmpty(req.Name)
            && string.IsNullOrEmpty(req.Surname)
        )
            throw new BadRequestException(
                "At least one of 'id', 'name'," +
                "or 'surname' must be provided."
            );
    }

    private static void ValidateSeason(int season)
    {
        if (season < 1946)
        {
            throw new BadRequestException("Season must be 1946 or later.");
        }
    }

    private static void ValidateResponseSize(PlayerResponseSize size)
    {
        if (!Enum.IsDefined(size))
        {
            throw new BadRequestException("Invalid player response size.");
        }
    }
}
