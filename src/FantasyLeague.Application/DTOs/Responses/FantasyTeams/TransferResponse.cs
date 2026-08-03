using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.DTOs.Responses.FantasyTeams;

public sealed record TeamRosterPlayerResponse(
    Guid Id, string FirstName, string LastName, string? NbaTeam, string? Position);

public sealed record TransferPlayerResponse(
    Guid PlayerId, Guid FromTeamId, string FirstName, string LastName);

public sealed record TransferResponse(
    Guid Id,
    Guid InitiatingTeamId,
    Guid CounterpartyTeamId,
    TransferStatus Status,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    IReadOnlyCollection<TransferPlayerResponse> Players);
