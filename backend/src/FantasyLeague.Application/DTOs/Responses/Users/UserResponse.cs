namespace FantasyLeague.Application.DTOs.Responses.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string TimeZoneId = "UTC");
