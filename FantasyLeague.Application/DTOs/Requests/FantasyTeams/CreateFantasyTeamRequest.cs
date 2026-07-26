namespace FantasyLeague.Application.DTOs.Requests.FantasyTeams;

public sealed record CreateFantasyTeamRequest(
    string Name,
    Guid LeagueId,
    Guid OwnerId
);
