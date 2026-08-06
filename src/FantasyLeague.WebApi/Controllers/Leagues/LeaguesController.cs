using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.Services.Leagues;
using Microsoft.AspNetCore.Mvc;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.WebApi.Controllers.Leagues;

[ApiController]
[Route("api/leagues")]
public sealed partial class LeaguesController(
    ILeagueService leagueService,
    IFantasyTeamService fantasyTeamService,
    ILogger<LeaguesController> logger) : ControllerBase
{
    [HttpGet("{id:guid}/standings")]
    [ProducesResponseType<IReadOnlyList<LeagueStandingResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeagueStandingResponse>>> GetStandingsAsync(
        Guid id,
        CancellationToken cancellation) =>
        Ok(await leagueService.GetStandingsAsync(id, cancellation));

    [HttpGet("{id:guid}/match-stats")]
    [ProducesResponseType<MatchStats>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MatchStats>> GetMatchStatsAsync(
        Guid id, [FromQuery] Guid homeTeamId, [FromQuery] Guid awayTeamId,
        CancellationToken cancellation) =>
        Ok(await leagueService.GetMatchStatsAsync(id, homeTeamId, awayTeamId, cancellation));


    /// <summary>
    /// Returns the generated fixtures for a league, ordered by week.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The league's home-and-away fixtures.</returns>
    /// <response code="200">The fixtures were retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet("{id:guid}/fixtures")]
    [ProducesResponseType<IReadOnlyList<LeagueFixtureResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LeagueFixtureResponse>>>
        GetFixturesAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return Ok(await leagueService.GetFixturesAsync(id, cancellation));
    }


    /// <summary>
    /// Returns the generated snake draft order for a league.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>All draft positions ordered by overall pick.</returns>
    /// <response code="200">The draft order was retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet("{id:guid}/draft-order")]
    [ProducesResponseType<IReadOnlyList<DraftPickOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DraftPickOrderResponse>>>
        GetDraftOrderAsync(
        Guid id,
        CancellationToken cancellation
    )
    {
        return Ok(await leagueService.GetDraftOrderAsync(id, cancellation));
    }


    /// <summary>
    /// Returns a paginated collection of leagues.
    /// </summary>
    /// <param name="request">Pagination options.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>A paginated collection of leagues.</returns>
    /// <response code="200">The leagues were retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<LeagueResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LeagueResponse>>> GetAsync(
        [FromQuery] PaginationRequest request,
        [FromQuery] LeagueStatus? status,
        CancellationToken cancellation)
    {
        var response = await leagueService.GetAsync(request, status, cancellation);
        return Ok(response);
    }


    /// <summary>
    /// Returns a league by identifier.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The requested league.</returns>
    /// <response code="200">The league was retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LeagueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeagueResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellation)
    {
        var response = await leagueService.GetByIdAsync(id, cancellation);
        return Ok(response);
    }


    /// <summary>
    /// Returns the fantasy teams that represent members of a league.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="request">Pagination options.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>A paginated collection of league members.</returns>
    /// <response code="200">The league members were retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet("{leagueId:guid}/members")]
    [ProducesResponseType<PagedResponse<FantasyTeamResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<FantasyTeamResponse>>> GetMembersAsync(
        Guid leagueId,
        [FromQuery] PaginationRequest request,
        CancellationToken cancellation)
    {
        var response = await fantasyTeamService.GetByLeagueIdAsync(
            leagueId, request, cancellation);

        return Ok(response);
    }


    /// <summary>
    /// Adds a fantasy team as a member of a league.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="request">The team name and owner used 
    /// to create the membership.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The fantasy team created for the league member.</returns>
    /// <response code="201">The member was added successfully.</response>
    /// <response code="400">The request data is invalid.</response>
    /// <response code="404">The league or owner was not found.</response>
    /// <response code="409">The league is full
    /// or the owner or team name is already in use.</response>
    [HttpPost("{leagueId:guid}/members")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> AddMemberAsync(
        Guid leagueId,
        [FromBody] AddLeagueMemberRequest request,
        CancellationToken cancellation)
    {
        var response = await fantasyTeamService.AddLeagueMemberAsync(
            leagueId, request, cancellation);

        LogTeamJoin(response.Id, leagueId, response.OwnerId);

        var result = CreatedAtAction(
            "GetById",
            "FantasyTeams",
            new { id = response.Id },
            response);

        return result;
    }


    /// <summary>
    /// Joins a league by its join code and creates a fantasy team for the member.
    /// </summary>
    /// <param name="request">The join code, team name, and owner information.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The fantasy team created for the new league member.</returns>
    /// <response code="201">The league was joined successfully.</response>
    /// <response code="400">The request data or join code is invalid.</response>
    /// <response code="404">The join code or owner was not found.</response>
    /// <response code="409">The league is full
    /// or the owner or team name is already in use.</response>
    [HttpPost("join")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> JoinAsync(
        [FromBody] JoinLeagueRequest request,
        CancellationToken cancellation)
    {
        var response = await fantasyTeamService.JoinLeagueAsync(
            request,
            cancellation
        );

        LogTeamJoinByCode(response.Id, response.LeagueId, response.OwnerId);

        var result = CreatedAtAction(
            "GetById",
            "FantasyTeams",
            new { id = response.Id },
            response
        );

        return result;
    }


    /// <summary>
    /// Removes a fantasy team and its membership from a league.
    /// </summary>
    /// <param name="leagueId">The league's unique identifier.</param>
    /// <param name="teamId">The fantasy team's unique identifier.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>An empty response.</returns>
    /// <response code="204">The member was removed successfully.</response>
    /// <response code="404">The league or fantasy team membership was not found.</response>
    [HttpDelete("{leagueId:guid}/members/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMemberAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellation)
    {
        await fantasyTeamService.RemoveLeagueMemberAsync(
            leagueId,
            teamId,
            cancellation
        );

        LogRemoveTeam(teamId, leagueId);

        return NoContent();
    }


    /// <summary>
    /// Creates a new league.
    /// </summary>
    /// <param name="request">The information required to create the league.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The newly created league.</returns>
    /// <response code="201">The league was created successfully.</response>
    /// <response code="400">The request data is invalid.</response>
    /// <response code="404">The specified commissioner was not found.</response>
    [HttpPost]
    [ProducesResponseType<LeagueResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeagueResponse>> CreateAsync(
        [FromBody] CreateLeagueRequest request,
        CancellationToken cancellation)
    {
        var response = await leagueService.CreateAsync(request, cancellation);

        LogPostLeague(response.Id, response.CommissionerId, response.Season);

        var result = CreatedAtAction(
            "GetById",
            new { id = response.Id },
            response
        );

        return result;
    }


    /// <summary>
    /// Updates an existing league.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="request">The updated league information.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>The updated league.</returns>
    /// <response code="200">The league was updated successfully.</response>
    /// <response code="400">The request data is invalid.</response>
    /// <response code="404">The specified league was not found.</response>
    /// <response code="409">The update conflicts with the current team count.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<LeagueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LeagueResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateLeagueRequest request,
        CancellationToken cancellation)
    {
        var response = await leagueService.UpdateAsync(id, request, cancellation);

        LogUpdateLeague(response.Id);

        return Ok(response);
    }


    /// <summary>
    /// Deletes a league by identifier.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellation">Token used to cancel the operation.</param>
    /// <returns>An empty response.</returns>
    /// <response code="204">The league was deleted successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        [FromQuery] Guid commissionerId,
        CancellationToken cancellation)
    {
        await leagueService.DeleteAsync(id, commissionerId, cancellation);
        LogDeleteLeague(id, commissionerId);
        return NoContent();
    }

    // ============================ Logging helper methods ============================

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Team {TeamId} joined league {LeagueId} for owner {OwnerId}."
    )]
    private partial void LogTeamJoin(Guid teamId, Guid leagueId, Guid ownerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Team {TeamId} joined league" +
        "{LeagueId} by join code for owner {OwnerId}."
    )]
    private partial void LogTeamJoinByCode(Guid teamId, Guid leagueId, Guid ownerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Team {TeamId} was removed from league {LeagueId}."
    )]
    private partial void LogRemoveTeam(Guid teamId, Guid leagueId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "League {LeagueId} was created" +
        "by commissioner {CommissionerId} for season {Season}."
    )]
    private partial void LogPostLeague(Guid leagueId, Guid commissionerId, int season);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "League {LeagueId} was updated."
    )]
    private partial void LogUpdateLeague(Guid leagueId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "League {LeagueId} was cancelled by commissioner {CommissionerId}."
    )]
    private partial void LogDeleteLeague(Guid leagueId, Guid commissionerId);

}
