using FantasyLeague.Application.DTOs.Requests.NbaPlayers;

namespace FantasyLeague.Application.Common.Normalization;

internal static class NbaPlayerNormalization
{
    public static void NormalizePlayerRequest(
        ref GetNbaPlayersRequest req
    )
    {
        req = req with
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
