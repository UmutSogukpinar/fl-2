using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Requests.FantasyTeams;
using FantasyLeague.Application.DTOs.Responses.FantasyTeams;
using FantasyLeague.Application.Services.FantasyTeams;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/fantasy-teams")]
public sealed class FantasyTeamsController(IFantasyTeamService teamService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<FantasyTeamResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<FantasyTeamResponse>>> GetByLeagueIdAsync(
        [FromQuery] Guid leagueId,
        CancellationToken cancellationToken)
    {
        var response = await teamService.GetByLeagueIdAsync(leagueId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FantasyTeamResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await teamService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> CreateAsync(
        [FromBody] CreateFantasyTeamRequest request,
        CancellationToken cancellationToken)
    {
        var response = await teamService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<FantasyTeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FantasyTeamResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateFantasyTeamRequest request,
        CancellationToken cancellationToken)
    {
        var response = await teamService.UpdateAsync(id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await teamService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
