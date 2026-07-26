namespace FantasyLeague.Application.DTOs.Responses.NbaPlayers;

public interface IPlayerResponse
{
    Guid Id { get; }

    string FirstName { get; }

    string LastName { get; }

    string? Team { get; }

    string? Position { get; }
}
