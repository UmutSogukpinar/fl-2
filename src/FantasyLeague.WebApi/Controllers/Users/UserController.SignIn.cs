using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Responses.Common;
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
        CancellationToken cancellationToken
    )
    {
        return Ok(await userService.SignInAsync(
                    request, cancellationToken)
            );
    }
}
