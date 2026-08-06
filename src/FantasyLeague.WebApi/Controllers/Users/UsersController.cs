using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Users;
using Microsoft.AspNetCore.Authorization;
using FantasyLeague.WebApi.Authorization;

namespace FantasyLeague.WebApi.Controllers.Users;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed partial class UsersController
    (IUserService userService) : ControllerBase
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">
    /// The information required to create the user.
    /// </param>
    /// <param name="cancellation">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>The newly created user.</returns>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellation)
    {
        var response = await userService.CreateAsync(
            request, cancellation
        );

        return CreatedAtAction(
            "GetById",
            new { id = response.Id },
            response);
    }


    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="request">The updated user information.</param>
    /// <param name="cancellation">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellation)
    {
        var response = await userService.UpdateAsync(
            id, request, cancellation
        );

        return Ok(response);
    }


    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellation">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>An empty response.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellation)
    {
        await userService.DeleteAsync(id, cancellation);

        return NoContent();
    }
}
