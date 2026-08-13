namespace FantasyLeague.Application.DTOs.Requests.FantasyTeams;

public sealed record CreateTransferRequest(
    Guid CounterpartyTeamId,
    IReadOnlyCollection<Guid> OfferedPlayerIds,
    IReadOnlyCollection<Guid> RequestedPlayerIds
);

public sealed record ApproveTransferRequest(Guid ApprovingTeamId);

public sealed record TransferCreatedResponse(Guid Id);
