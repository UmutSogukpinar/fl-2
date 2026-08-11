namespace FantasyLeague.Application.DTOs.Responses.FantasyTeams;

public sealed record FantasyTeamResponse(
    Guid Id,
    string Name,
    Guid LeagueId,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
