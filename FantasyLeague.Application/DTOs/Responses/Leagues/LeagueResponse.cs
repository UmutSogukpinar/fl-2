namespace FantasyLeague.Application.DTOs.Responses.Leagues;

using FantasyLeague.Domain.Enums;

public sealed record LeagueResponse(
    Guid Id,
    string Name,
    string? Description,
    int Season,
    int MaxTeams,
    Guid CommissionerId,
    LeagueStatus Status,
    DateTime? DraftDate,
    string JoinCode,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
