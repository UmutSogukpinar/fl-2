using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    public async Task<UserResponse> UpdateAsync(
        Guid id,
        UpdateUserRequest req,
        CancellationToken cancellation = default)
    {
        var user = await GetTrackedUserOrThrowAsync(id, cancellation);

        req = req.NormalizeUpdateUserRequest();
        req.ValidateUpdateUserRequest();

        await EnsureUniqueAsync(
            req.Username,
            req.Email,
            id,
            cancellation);

        req.MapTo(user);

        await _userRepository.SaveChangesAsync(cancellation);
        return user.ToResponse();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        var user = await GetTrackedUserOrThrowAsync(id, cancellation);
        _userRepository.Remove(user);
        await _userRepository.SaveChangesAsync(cancellation);
    }

    private async Task<User> GetTrackedUserOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _userRepository.GetTrackedByIdAsync(
            id, cancellation)
            ?? throw new NotFoundException(
                $"User '{id}' was not found.");
    }
}
