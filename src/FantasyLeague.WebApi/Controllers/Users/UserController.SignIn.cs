using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using Microsoft.AspNetCore.Mvc;

namespace FantasyLeague.WebApi.Controllers.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Verifies an existing user's credentials.
    /// </summary>
    /// <param name="request">
    ///     The email address and password to verify.
    /// </param>
    /// <param name="cancellationToken">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>The authenticated user's profile.</returns>
    /// <response code="200">
    ///     The credentials were verified successfully.
    /// </response>
    /// <response code="401">
    ///     The email address or password is incorrect.
    /// </response>
    [HttpPost("sign-in")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> SignInAsync(
        [FromBody] SignInRequest request,
        CancellationToken cancellation = default
    )
    {
        var (result, accessToken, refreshToken) = await
            userService.SignInAsync(request, cancellation);

        SetCookies(accessToken, refreshToken);

        return Ok(result);
    }

    private void SetCookies(
        string accessToken,
        string refreshToken
    )
    {
        // TODO: Remove magic values for cookie expiration times
        // and move them to configuration.

        Response.Cookies.Append(
            "access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            }
        );

        Response.Cookies.Append(
            "refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = true,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            }
        );
    }
}
