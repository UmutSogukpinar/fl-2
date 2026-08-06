using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;

namespace FantasyLeague.Application.Services.Auth;

public interface IAuthService
{
    Task<(UserResponse user, string accessToken, string refreshToken)> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default);

    Task<(UserResponse user, string accessToken, string refreshToken)> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
