using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Application.Common.Time;

namespace FantasyLeague.Application.Mappings;

public static class UserMappings
{
    public static User ToEntity(this CreateUserRequest request, string passwordHash) => new()
    {
        Username = NormalizeUsername(request.Username),
        Email = NormalizeEmail(request.Email),
        Password = passwordHash,
        TimeZoneId = LocationTimeZoneResolver.Resolve(request.Location)
    };

    public static void MapTo(this UpdateUserRequest request, User user)
    {
        user.Username = NormalizeUsername(request.Username);
        user.Email = NormalizeEmail(request.Email);
        if (request.Location is not null)
        {
            user.TimeZoneId = LocationTimeZoneResolver.Resolve(request.Location);
        }
        user.UpdatedAt = DateTime.UtcNow;
    }

    public static UserResponse ToResponse(this User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.CreatedAt,
        user.UpdatedAt,
        user.TimeZoneId);

    private static string NormalizeUsername(string username) => username.Trim();

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
