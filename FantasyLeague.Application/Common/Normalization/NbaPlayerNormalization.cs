using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;

namespace FantasyLeague.Application.Common.Normalization;

internal static class NbaPlayerNormalization
{
    public static GetNbaPlayersRequest NormalizePlayerRequest(
        this GetNbaPlayersRequest req
    )
    {
        if (req is null)
            throw new BadRequestException(
                "GetNbaPlayerRequest Object is null!"
            );

        return req with
        {
            Name = NormalizeName(req.Name),
            Surname = NormalizeName(req.Surname)
        };
    }

    // ======================== Utils ========================

    private static string NormalizeName(string? name)
    {
        return name?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}
