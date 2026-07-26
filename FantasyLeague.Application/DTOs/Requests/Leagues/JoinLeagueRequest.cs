namespace FantasyLeague.Application.DTOs.Requests.Leagues;

public sealed record JoinLeagueRequest(
    string JoinCode,
    string TeamName,
    Guid OwnerId);
