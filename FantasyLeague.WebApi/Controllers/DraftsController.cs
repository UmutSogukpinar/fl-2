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
    IHubContext<FantasyLeagueHub> hubContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DraftStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DraftStateResponse>> GetStateAsync(
        Guid leagueId,
        CancellationToken cancellationToken)
    {
        return Ok(await draftService.GetStateAsync(leagueId, cancellationToken));
    }

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
        return Ok(state);
    }

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
        return Ok(state);
    }
}
