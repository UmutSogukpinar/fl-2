namespace FantasyLeague.Application.DTOs.Responses.Leagues;

public sealed record LeagueFixtureResponse(
    Guid Id,
    Guid LeagueId,
    int Week,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName);
