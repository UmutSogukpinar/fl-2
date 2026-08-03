namespace FantasyLeague.Application.DTOs.Requests.Users;

public sealed record SignInRequest(
    string Email,
    string Password);
