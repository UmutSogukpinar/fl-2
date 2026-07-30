using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.DTOs.Responses.Leagues;

public sealed record LeagueFixtureResponse(
    long Id,
    Guid LeagueId,
    int Week,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    int? HomeScore,
    int? AwayScore,
    DateTime? GameTime,
    MatchStatus Status);
