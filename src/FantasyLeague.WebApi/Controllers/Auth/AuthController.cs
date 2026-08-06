using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FantasyLeague.WebApi.Controllers.Auth;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("sign-in")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> SignInAsync(
        [FromBody] SignInRequest request,
        CancellationToken cancellationToken = default)
    {
        var (user, accessToken, refreshToken) = await authService.SignInAsync(
            request,
            cancellationToken);

        SetCookies(accessToken, refreshToken);
        return Ok(user);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        var (user, accessToken, refreshToken) = await authService.RefreshAsync(
            Request.Cookies["refresh_token"] ?? string.Empty,
            cancellationToken);

        SetCookies(accessToken, refreshToken);
        return Ok(user);
    }

    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOutAsync(
        CancellationToken cancellationToken = default)
    {
        await authService.SignOutAsync(
            Request.Cookies["refresh_token"] ?? string.Empty,
            cancellationToken);

        Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
        Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });

        return NoContent();
    }

    private void SetCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append("access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = true,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
