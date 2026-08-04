using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Requests.FantasyTeams;

namespace FantasyLeague.Application.Common.Validation;

internal static class TransferValidation
{
    public static void ValidateCreateTransferRequest(
        this CreateTransferRequest request,
        Guid initiatingTeamId)
    {
        ValidateRequiredId(initiatingTeamId, "InitiatingTeamId");
        ValidateRequiredId(request.CounterpartyTeamId, "CounterpartyTeamId");

        if (initiatingTeamId == request.CounterpartyTeamId)
            throw new BadRequestException("A team cannot transfer players with itself.");

        ValidatePlayerIds(request.OfferedPlayerIds, "OfferedPlayerIds");
        ValidatePlayerIds(request.RequestedPlayerIds, "RequestedPlayerIds");
    }

    public static void ValidateApproveTransferRequest(Guid transferId, Guid approvingTeamId)
    {
        ValidateRequiredId(transferId, "TransferId");
        ValidateRequiredId(approvingTeamId, "ApprovingTeamId");
    }

    public static void ValidateReleasePlayerRequest(Guid teamId, Guid playerId)
    {
        ValidateRequiredId(teamId, "TeamId");
        ValidateRequiredId(playerId, "PlayerId");
    }

    public static void ValidateAddPlayerFromPoolRequest(Guid teamId, Guid playerId)
    {
        ValidateRequiredId(teamId, "TeamId");
        ValidateRequiredId(playerId, "PlayerId");
    }

    private static void ValidatePlayerIds(IReadOnlyCollection<Guid> playerIds, string fieldName)
    {
        if (playerIds.Count == 0)
            throw new BadRequestException($"{fieldName} must contain at least one player.");

        if (playerIds.Any(id => id == Guid.Empty))
            throw new BadRequestException($"{fieldName} contains an invalid player identifier.");

        if (playerIds.Count != playerIds.Distinct().Count())
            throw new BadRequestException($"{fieldName} cannot contain duplicate players.");
    }

    private static void ValidateRequiredId(Guid id, string fieldName)
    {
        if (id == Guid.Empty)
            throw new BadRequestException($"{fieldName} is required.");
    }
}
