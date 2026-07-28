using FantasyLeague.Application.DTOs.Requests.Drafts;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Domain.Enums;
using FantasyLeague.WebApi.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/leagues/{leagueId:guid}/draft")]
public sealed class DraftsController(
    IDraftService draftService,
    IHubContext<FantasyLeagueHub> hubContext,
    ILogger<DraftsController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the current draft state and complete pick history for a league.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The current draft state, active pick, and completed picks.</returns>
    /// <response code="200">The draft state was retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet]
    [ProducesResponseType<DraftStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DraftStateResponse>> GetStateAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return Ok(await draftService.GetStateAsync(leagueId, cancellationToken));
    }

    /// <summary>
    /// Closes a league whose draft was delayed because minimum capacity was not reached.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="request">The commissioner identity used to authorize the operation.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The final state of the closed league draft.</returns>
    /// <response code="200">The delayed league was closed successfully.</response>
    /// <response code="403">The requester is not the league commissioner.</response>
    /// <response code="409">The league is not in the delayed state.</response>
    [HttpPost("close")]
    [ProducesResponseType<DraftStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DraftStateResponse>> CloseDelayedLeagueAsync(
        Guid leagueId,
        [FromBody] CloseDelayedLeagueRequest request,
        CancellationToken cancellationToken)
    {
        var state = await draftService.CloseDelayedLeagueAsync(
            leagueId, request.CommissionerId, cancellationToken);
        await hubContext.Clients.Group(FantasyLeagueHub.LeagueGroup(leagueId))
            .SendAsync("LeagueClosed", state, cancellationToken);
        logger.LogInformation(
            "Delayed league {LeagueId} was closed by commissioner {CommissionerId}.",
            leagueId, request.CommissionerId);
        return Ok(state);
    }

    /// <summary>
    /// Selects an NBA player for the team that currently owns the draft turn.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="request">The team, owner, and NBA player selection.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated draft state and next available pick.</returns>
    /// <response code="200">The player was drafted successfully.</response>
    /// <response code="403">The requester does not own the team.</response>
    /// <response code="404">The league, team, or NBA player was not found.</response>
    /// <response code="409">The draft is inactive, the turn is invalid, or the player was already selected.</response>
    [HttpPost("picks")]
    [ProducesResponseType<DraftStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DraftStateResponse>> MakePickAsync(
        Guid leagueId,
        [FromBody] MakeDraftPickRequest request,
        CancellationToken cancellationToken)
    {
        var state = await draftService.MakePickAsync(leagueId, request, cancellationToken);
        var eventName = state.Status == LeagueStatus.Active ? "DraftCompleted" : "DraftUpdated";
        await hubContext.Clients.Group(FantasyLeagueHub.LeagueGroup(leagueId))
            .SendAsync(eventName, state, cancellationToken);
        logger.LogInformation(
            "Draft pick {CompletedPicks}/{TotalPicks} completed in league {LeagueId} by team {TeamId} for NBA player {NbaPlayerId}.",
            state.CompletedPicks, state.TotalPicks, leagueId, request.TeamId, request.NbaPlayerId);
        return Ok(state);
    }
}
