using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Users;
using FantasyLeague.WebApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FantasyLeague.WebApi.Controllers.Users;

public sealed partial class UsersController
{
    /// <summary>
    /// Returns a paginated list of users.
    /// </summary>
    /// <param name="request">Pagination options.</param>
    /// <param name="cancellation">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>A paginated collection of users.</returns>
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAsync(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellation)
    {
        var response = await userService.GetAsync(request, cancellation);
        return Ok(response);
    }


    /// <summary>
    /// Returns a user by identifier.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellation">
    ///     Token used to cancel the operation.
    /// </param>
    /// <returns>The requested user.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellation)
    {
        var response = await userService.GetByIdAsync(
            id, cancellation);

        return Ok(response);
    }
}
