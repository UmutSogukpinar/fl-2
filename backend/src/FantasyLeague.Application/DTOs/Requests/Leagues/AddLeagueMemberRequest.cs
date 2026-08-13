namespace FantasyLeague.Application.DTOs.Requests.Leagues;

public sealed record AddLeagueMemberRequest(
    string TeamName,
    Guid OwnerId);
