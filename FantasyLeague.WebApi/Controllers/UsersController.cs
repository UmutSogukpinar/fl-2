using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Requests.Users;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.Application.Services.Users;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of users.
    /// </summary>
    /// <param name="request">Pagination options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A paginated collection of users.</returns>
    [HttpGet]
    [ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAsync(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.GetAsync(request, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Returns a user by identifier.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The requested user.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await userService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="request">The information required to create the user.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The newly created user.</returns>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = response.Id },
            response);
    }


    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="request">The updated user information.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated user.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await userService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Deletes a user.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An empty response.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
