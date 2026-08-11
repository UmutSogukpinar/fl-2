namespace FantasyLeague.Application.DTOs.Responses.Drafts;

public sealed record DraftPickResponse(
    Guid Id,
    Guid TeamId,
    string TeamName,
    int Round,
    int PositionInRound,
    int OverallPick,
    Guid? NbaPlayerId,
    string? NbaPlayerName,
    DateTime? PickedAt);
