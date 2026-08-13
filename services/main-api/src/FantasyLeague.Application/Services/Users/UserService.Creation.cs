using FantasyLeague.Domain.Entities.Users;

using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Mappings;

namespace FantasyLeague.Application.Services.Users;

public sealed partial class UserService
{
    public async Task<UserResponse> CreateAsync(
        CreateUserRequest req,
        CancellationToken cancellation = default)
    {
        req = req.NormalizeCreateUserRequest();
        req.ValidateCreateUserRequest();

        await EnsureUniqueAsync(
            req.Username,
            req.Email,
            null,
            cancellation);

        var user = req.ToEntity(_passwordHasher.Hash(req.Password));

        _userRepository.Add(user);
        await _userRepository.SaveChangesAsync(cancellation);

        return user.ToResponse();
    }
}
