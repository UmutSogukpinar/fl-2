namespace FantasyLeague.Application.DTOs.Requests.Users;

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string Password
);
