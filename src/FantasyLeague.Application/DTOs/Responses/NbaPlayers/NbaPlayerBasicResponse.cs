namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public sealed record NbaPlayerBasicResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Team,
    string Position
) : IPlayerResponse;
