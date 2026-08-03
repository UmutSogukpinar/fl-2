using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Services.FantasyTeams;
using Microsoft.AspNetCore.Mvc;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/fantasy-teams")]
public sealed class FantasyTeamsController(IFantasyTeamService teamService)
    : ControllerBase
{
    [HttpGet("{id:guid}/players")]
    public async Task<ActionResult<IReadOnlyCollection<TeamRosterPlayerResponse>>> GetRosterPlayersAsync(
        Guid id, CancellationToken cancellation) =>
        Ok(await teamService.GetRosterPlayersAsync(id, cancellation));

    [HttpGet("{id:guid}/transfers")]
    public async Task<ActionResult<IReadOnlyCollection<TransferResponse>>> GetTransfersAsync(
        Guid id, CancellationToken cancellation) =>
        Ok(await teamService.GetTransfersAsync(id, cancellation));

    /// <summary>
    /// Retrieves all fantasy teams belonging to the specified league.
    /// </summary>
    /// <param name="leagueId">
    /// The unique identifier of the league whose fantasy teams will be retrieved.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation when the HTTP request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response containing a read-only collection of fantasy teams.
    /// </returns>
    /// <response code="200">
    /// The fantasy teams were retrieved successfully.
    /// </response>
    /// <response code="404">
    /// The specified league was not found.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// GET /api/fantasy-teams?leagueId=3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType<PagedResponse<FantasyTeamResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<FantasyTeamResponse>>>
    GetByLeagueIdAsync(
        [FromQuery] Guid leagueId,
        [FromQuery] PaginationRequest request,
        CancellationToken cancellation
    )
    {
        var response = await teamService.GetByLeagueIdAsync(
            leagueId, request, cancellation
        );

        return Ok(response);
    }


    /// <summary>
    /// Retrieves a fantasy team by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the fantasy team to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation when the HTTP request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response containing the requested fantasy team.
    /// </returns>
    /// <response code="200">
    /// The fantasy team was retrieved successfully.
    /// </response>
    /// <response code="404">
    /// A fantasy team with the specified identifier was not found.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// GET /api/FantasyTeams/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </code>
    /// </example>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FantasyTeamResponse>> GetByIdAsync(
        [FromRoute] Guid id,
        CancellationToken cancellation)
    {
        var response = await teamService.GetByIdAsync(
            id,
            cancellation
        );

        return Ok(response);
    }


    /// <summary>
    /// Updates an existing fantasy team.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the fantasy team to update.
    /// </param>
    /// <param name="request">
    /// The updated information for the fantasy team.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation when the HTTP request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response containing the updated fantasy team.
    /// </returns>
    /// <response code="200">
    /// The fantasy team was updated successfully.
    /// </response>
    /// <response code="400">
    /// The request data is invalid.
    /// </response>
    /// <response code="404">
    /// A fantasy team with the specified identifier was not found.
    /// </response>
    /// <response code="409">
    /// The update conflicts with an existing resource.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// PUT /api/FantasyTeams/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// {
    ///   "name": "Istanbul Warriors"
    /// }
    /// </code>
    /// </example>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateFantasyTeamRequest request,
        CancellationToken cancellation)
    {
        var response = await teamService.UpdateAsync(
            id, request, cancellation
        );

        return Ok(response);
    }


    /// <summary>
    /// Deletes a fantasy team by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the fantasy team to delete.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the operation when the HTTP request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response indicating the result of the delete operation.
    /// </returns>
    /// <response code="204">
    /// The fantasy team was deleted successfully.
    /// </response>
    /// <response code="404">
    /// A fantasy team with the specified identifier was not found.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// DELETE /api/FantasyTeams/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// </code>
    /// </example>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        await teamService.DeleteAsync(id, cancellation);
        return NoContent();
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseAPlayerAsync(
        Guid id,
        [FromQuery] Guid PlayerId,
        CancellationToken cancellation
    )
    {
        await teamService.ReleaseAPlayerAsync(id, PlayerId, cancellation);

        return NoContent();
    }

    [HttpPost("{id:guid}/transfers")]
    [ProducesResponseType<TransferCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransferCreatedResponse>> CreateTransferAsync(
        [FromRoute] Guid id,
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellation
    )
    {
        var transferId = await teamService.CreateTransferAsync(id, request, cancellation);
        return StatusCode(StatusCodes.Status201Created, new TransferCreatedResponse(transferId));
    }

    [HttpPatch("transfers/{transferId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveTransferAsync(
        [FromRoute] Guid transferId,
        [FromBody] ApproveTransferRequest request,
        CancellationToken cancellation)
    {
        await teamService.ApproveTransferAsync(
            transferId, request.ApprovingTeamId, cancellation);
        return NoContent();
    }

}
