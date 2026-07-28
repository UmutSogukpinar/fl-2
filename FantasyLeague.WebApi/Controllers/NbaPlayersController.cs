using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Services.NbaPlayers;
using Microsoft.AspNetCore.Mvc;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/nba-players")]
public sealed partial class NbaPlayersController(
    INbaPlayerSyncService _syncService,
    INbaPlayerService _nbaPlayerService,
    ILogger<NbaPlayersController> logger
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

        LogNbaPlayerSyncCompleted(
            response.Season,
            response.CreatedCount,
            response.UpdatedCount,
            response.StatisticsProcessedCount
        );

        return Ok(response);
    }


    /// <summary>
    /// Returns a paginated collection of NBA players.
    /// </summary>
    /// <param name="request">Pagination options.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A page of NBA players ordered by first and last name.</returns>
    /// <response code="200">The NBA players were retrieved successfully.</response>
    /// <response code="400">The pagination options are invalid.</response>
    /// <example Example Request:
    /// <code>
    /// GET /api/nba-players/3fa85f64-5717-4562-b3fc-2c963f66afa6?season=2024&amp;size=Extended
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType<PagedResponse<NbaPlayerBasicResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<NbaPlayerBasicResponse>>> GetAsync(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _nbaPlayerService.GetAsync(request, cancellationToken);

        return Ok(response);
    }


    /// <summary>
    /// Retrieves an NBA player for the specified season
    /// using the requested response detail level.
    /// </summary>
    /// <param name="id">The unique identifier of the NBA player.</param>
    /// <param name="season">
    /// The season for which the player's statistics are retrieved.
    /// The default value is <c>2024</c>.
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
    /// GET /api/nba-players/3fa85f64-5717-4562-b3fc-2c963f66afa6?season=2024&amp;size=Extended
    /// </code>
    /// </example>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IPlayerResponse>> GetNbaPlayerByIdAndYearAsync(
        Guid id,
        [FromQuery] int season = 2024,
        [FromQuery] PlayerResponseSize size = PlayerResponseSize.Basic,
        CancellationToken cancellation = default
    )
    {
        var response = await _nbaPlayerService.GetNbaPlayerByIdAndYearAsync(
                id,
                season,
                size,
                cancellation
        );

        return Ok(response);
    }

    /// <summary>
    /// Retrieves NBA players whose names match the specified search value,
    /// using the requested response detail level.
    /// </summary>
    /// <param name="name">
    /// The full or partial name of the NBA player or players to search for.
    /// </param>
    /// <param name="surname">
    /// The full or partial surname of the NBA player or players to search for.
    /// </param>
    /// <param name="season">
    /// The season for which the players' statistics are retrieved.
    /// The default value is <c>2024</c>.
    /// </param>
    /// <param name="size">
    /// The detail level of the player responses.
    /// The default value is <see cref="PlayerResponseSize.Basic"/>.
    /// </param>
    /// <param name="cancellation">
    /// A token used to cancel the operation if the request is aborted.
    /// </param>
    /// <returns>
    /// An HTTP response containing all NBA players whose names match the
    /// specified search value at the requested detail level.
    /// </returns>
    /// <response code="200">
    /// One or more matching players were found and returned successfully.
    /// </response>
    /// <response code="400">
    /// The provided query parameters are invalid.
    /// </response>
    /// <response code="404">
    /// No players matching the specified name were found, or no statistics
    /// were available for the requested season.
    /// </response>
    /// <example>
    /// Example request:
    /// <code>
    /// GET /api/nba-players?name=LeBron&amp;surname=James&amp;season=2024&amp;size=basic
    /// </code>
    /// </example>
    ///
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<IPlayerResponse>>>
    GetNbaPlayersByNameAndYearAsync(
        [FromQuery] PaginationRequest pagination,
        [FromQuery] GetNbaPlayersRequest req,
        CancellationToken cancellation = default
    )
    {
        var response = await _nbaPlayerService.GetNbaPlayersByNameAndYearAsync(
                pagination,
                req,
                cancellation
        );

        return Ok(response);
    }

    // ============================ Logging helper methods ============================

     [LoggerMessage(
        Level = LogLevel.Information,
        Message =
            "NBA player sync completed for season {Season}: " +
            "{CreatedCount} created, {UpdatedCount} updated, " +
            "{StatisticsCount} statistics.")]
    private partial void LogNbaPlayerSyncCompleted(
        int season,
        int createdCount,
        int updatedCount,
        int statisticsCount);
}
