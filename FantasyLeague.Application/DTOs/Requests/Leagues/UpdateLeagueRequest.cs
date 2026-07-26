namespace FantasyLeague.Application.DTOs.Requests.Leagues;

public sealed record UpdateLeagueRequest(
    string Name,
    string? Description,
    int MaxTeams,
    DateTime? DraftDate = null,
    int RosterSize = 13);
