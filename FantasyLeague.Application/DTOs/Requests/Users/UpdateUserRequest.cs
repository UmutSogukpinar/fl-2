namespace FantasyLeague.Application.DTOs.Requests.Users;

public sealed record UpdateUserRequest(
    string Username,
    string Email,
    string? Location = null
);
