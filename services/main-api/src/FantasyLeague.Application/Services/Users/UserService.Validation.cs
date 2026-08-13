using FantasyLeague.Application.Common.Exceptions;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    private async Task EnsureUniqueAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellation)
    {
        if (await _userRepository.ExistsAsync(
                username,
                email,
                excludedUserId,
                cancellation))
        {
            throw new ConflictException(
                "The username or email is already in use.");
        }
    }
}
