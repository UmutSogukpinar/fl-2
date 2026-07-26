using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Services.NbaPlayers;
using FantasyLeague.Application.Models;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/nba-players")]
public sealed class NbaPlayersController(
    INbaPlayerSyncService _syncService,
    INbaPlayerService _nbaPlayerService
) : ControllerBase
{
    /// <summary>
    /// Synchronizes active NBA players and their statistics from the external provider.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A summary of the synchronization operation.</returns>
    /// <response code="200">The synchronization completed successfully.</response>
    /// <response code="502">The external NBA provider could not be reached.</response>
    [HttpPost("sync")]
    [ProducesResponseType<NbaPlayerSyncResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<NbaPlayerSyncResponse>> SyncAsync(
        CancellationToken cancellationToken)
    {
        var response = await _syncService.SyncActivePlayersAsync(cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Retrieves an NBA player for the specified season
    /// using the requested response detail level.
    /// </summary>
    /// <param name="id">The unique identifier of the NBA player.</param>
    /// <param name="season">
    /// The season for which the player's statistics are retrieved.
    /// The default value is <c>2025</c>.
    /// </param>
    /// <param name="size">
    /// The detail level of the player response.
    /// The default value is <see cref="PlayerResponseSize.Basic"/>.
    /// </param>
    /// <param name="cancellation">
    /// A token used to cancel the operation if the request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response containing the player information at the requested detail level.
    /// </returns>
    /// <response code="200">The player was found and returned successfully.</response>
    /// <response code="400">The provided query parameters are invalid.</response>
    /// <response code="404">
    /// The specified player or the player's statistics
    /// for the requested season were not found.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// GET /api/nba-players?id=3fa85f64-5717-4562-b3fc-2c963f66afa6&amp;season=2025&amp;size=Extended
    /// </code>
    /// </example>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IPlayerResponse>>GetNbaPlayerByIdAndYearAsync(
        [FromQuery] Guid id,
        [FromQuery] int season = 2025,
        [FromQuery] PlayerResponseSize size = PlayerResponseSize.Basic,
        CancellationToken cancellation = default
    ){
        var response = await _nbaPlayerService.GetNbaPlayerByIdAndYearAsync(
                id, season, size, cancellation
            );

        return Ok(response);
    }
}
