namespace FantasyLeague.Application.DTOs.Requests.Leagues;

public sealed record CreateLeagueRequest(
    string Name,
    string? Description,
    int Season,
    int MaxTeams,
    Guid CommissionerId,
    DateTime DraftDate,
    int RosterSize = 13,
    string? TeamName = null);
