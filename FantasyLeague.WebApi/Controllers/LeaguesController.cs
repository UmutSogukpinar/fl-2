using Microsoft.AspNetCore.Mvc;

using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Services.Leagues;

namespace FantasyLeague.WebApi.Controllers;

[ApiController]
[Route("api/leagues")]
public sealed class LeaguesController(ILeagueService leagueService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<LeagueResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LeagueResponse>>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var response = await leagueService.GetAllAsync(cancellationToken);
        return Ok(response);
    }

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

    [HttpPost]
    [ProducesResponseType<LeagueResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeagueResponse>> CreateAsync(
        [FromBody] CreateLeagueRequest request,
        CancellationToken cancellationToken)
    {
        var response = await leagueService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await leagueService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
