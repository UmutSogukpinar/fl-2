namespace FantasyLeague.Application.DTOs.Responses.Leagues;

public sealed record LeagueStandingResponse(
    int Position, Guid TeamId, string TeamName, int Played, int Won,
    int Drawn, int Lost, int PointsFor, int PointsAgainst,
    int PointDifference, int Points);
