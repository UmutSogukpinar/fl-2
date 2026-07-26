using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Application.Services.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/leagues")]
public sealed class LeaguesController(
    ILeagueService leagueService,
    IFantasyTeamService fantasyTeamService) : ControllerBase
{

    /// <summary>
    /// Returns all leagues.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only collection of leagues.</returns>
    /// <response code="200">The leagues were retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<LeagueResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LeagueResponse>>> GetAsync(
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await leagueService.GetAsync(request, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Returns a league by identifier.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The requested league.</returns>
    /// <response code="200">The league was retrieved successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LeagueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeagueResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await leagueService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }


    [HttpGet("{leagueId:guid}/members")]
    [ProducesResponseType<PagedResponse<FantasyTeamResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<FantasyTeamResponse>>> GetMembersAsync(
        Guid leagueId,
        [FromQuery] PaginationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await fantasyTeamService.GetByLeagueIdAsync(
            leagueId, request, cancellationToken);

        return Ok(response);
    }


    [HttpPost("{leagueId:guid}/members")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> AddMemberAsync(
        Guid leagueId,
        [FromBody] AddLeagueMemberRequest request,
        CancellationToken cancellationToken)
    {
        var response = await fantasyTeamService.AddLeagueMemberAsync(
            leagueId, request, cancellationToken);

        var result = CreatedAtAction(
            nameof(FantasyTeamsController.GetByIdAsync),
            "FantasyTeams",
            new { id = response.Id },
            response);

        return result;
    }


    [HttpPost("join")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> JoinAsync(
        [FromBody] JoinLeagueRequest request,
        CancellationToken cancellationToken)
    {
        var response = await fantasyTeamService.JoinLeagueAsync(request, cancellationToken);

        var result = CreatedAtAction(
            nameof(FantasyTeamsController.GetByIdAsync),
            "FantasyTeams",
            new { id = response.Id },
            response
        );

        return result;
    }

    [HttpDelete("{leagueId:guid}/members/{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMemberAsync(
        Guid leagueId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        await fantasyTeamService.RemoveLeagueMemberAsync(
            leagueId,
            teamId,
            cancellationToken
        );

        return NoContent();
    }


    /// <summary>
    /// Creates a new league.
    /// </summary>
    /// <param name="request">The information required to create the league.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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
        CancellationToken cancellationToken)
    {
        var response = await leagueService.CreateAsync(request, cancellationToken);

        var result = CreatedAtAction(
            nameof(GetByIdAsync),
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
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
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
        CancellationToken cancellationToken)
    {
        var response = await leagueService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }


    /// <summary>
    /// Deletes a league by identifier.
    /// </summary>
    /// <param name="id">The league's unique identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An empty response.</returns>
    /// <response code="204">The league was deleted successfully.</response>
    /// <response code="404">The specified league was not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await leagueService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
