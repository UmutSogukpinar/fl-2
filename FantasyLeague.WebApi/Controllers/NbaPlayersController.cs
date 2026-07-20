using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Services.NbaPlayers;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/nba-players")]
public sealed class NbaPlayersController(INbaPlayerSyncService syncService) : ControllerBase
{
    [HttpPost("sync")]
    [ProducesResponseType<NbaPlayerSyncResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<NbaPlayerSyncResponse>> SyncAsync(
        CancellationToken cancellationToken)
    {
        var response = await syncService.SyncActivePlayersAsync(cancellationToken);
        return Ok(response);
    }
}
