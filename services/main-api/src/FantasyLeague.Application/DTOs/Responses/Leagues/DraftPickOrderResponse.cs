namespace FantasyLeague.Application.DTOs.Responses.Leagues;

public sealed record DraftPickOrderResponse(
    Guid Id,
    Guid LeagueId,
    Guid TeamId,
    string TeamName,
    int Round,
    int PositionInRound,
    int OverallPick);
