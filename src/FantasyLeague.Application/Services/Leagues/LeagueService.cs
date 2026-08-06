using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService(
    ILeagueRepository _leagueRepository,
    IFantasyTeamRepository _teamRepository,
    ILeagueSetupRepository _leagueSetupRepository,
    IUserRepository _userRepository,
    INbaPlayerRepository _playerRepository,
    ILogger<LeagueService> _logger
) : ILeagueService
{
    private async Task<LeagueResponse> GetLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _leagueRepository.GetResponseByIdAsync(
            id,
            cancellation)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }

    private async Task<League> GetTrackedLeagueOrThrowAsync(
        Guid id,
        CancellationToken cancellation)
    {
        return await _leagueRepository.GetTrackedByIdAsync(
            id,
            cancellation)
            ?? throw new NotFoundException($"League '{id}' was not found.");
    }
}
