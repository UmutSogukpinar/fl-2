using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

namespace FantasyLeague.Application.Common.Normalization;

internal static class TransferNormalization
{
    public static CreateTransferRequest NormalizeCreateTransferRequest(
        this CreateTransferRequest? request)
    {
        if (request is null)
            throw new BadRequestException("Request body is required.");

        return request with
        {
            OfferedPlayerIds = request.OfferedPlayerIds?.ToArray() ?? [],
            RequestedPlayerIds = request.RequestedPlayerIds?.ToArray() ?? []
        };
    }
}
