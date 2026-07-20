namespace FantasyLeague.Application.DTOs.Responses.Leagues;

public sealed record LeagueResponse(
    Guid Id,
    string Name,
    string? Description,
    int Season,
    int MaxTeams,
    Guid CommissionerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
