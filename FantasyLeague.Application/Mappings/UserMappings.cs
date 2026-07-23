using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Mappings;

public static class UserMappings
{
    public static User ToEntity(this CreateUserRequest request, string passwordHash) => new()
    {
        Username = NormalizeUsername(request.Username),
        Email = NormalizeEmail(request.Email),
        Password = passwordHash
    };

    public static void MapTo(this UpdateUserRequest request, User user)
    {
        user.Username = NormalizeUsername(request.Username);
        user.Email = NormalizeEmail(request.Email);
        user.UpdatedAt = DateTime.UtcNow;
    }

    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.CreatedAt,
        user.UpdatedAt);

    private static string NormalizeUsername(string username) => username.Trim();

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
