using System.Linq.Expressions;

using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Infrastructure.Repositories.Projections;

internal static class UserProjections
{
    internal static readonly Expression<Func<User, UserResponse>> Response =
        user => new UserResponse(
            user.Id,
            user.Username,
            user.Email,
            user.CreatedAt,
            user.UpdatedAt,
            user.TimeZoneId);
}
