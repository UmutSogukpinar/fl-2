using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Models;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<MatchStats> GetMatchStatsAsync(
        Guid leagueId,
        Guid homeTeamId,
        Guid awayTeamId,
        CancellationToken cancellation = default)
    {
        if (homeTeamId == awayTeamId)
        {
            throw new BadRequestException(
                "Home and away teams must be different."
            );
        }

        var league = await _leagueRepository.GetResponseByIdAsync(
            leagueId, cancellation)
            ?? throw new NotFoundException(
                    $"League '{leagueId}' was not found."
                );

        await EnsureTeamBelongsToLeagueAsync(
            homeTeamId, leagueId, "Home", cancellation
        );
        await EnsureTeamBelongsToLeagueAsync(
            awayTeamId, leagueId, "Away", cancellation
        );

        return await _playerRepository.GetMatchStatsByTeamIdsAsync(
            leagueId,
            homeTeamId,
            awayTeamId,
            league.Season,
            cancellation
        );
    }

    private async Task EnsureTeamBelongsToLeagueAsync(
        Guid teamId,
        Guid leagueId,
        string role,
        CancellationToken cancellation)
    {
        var team = await _teamRepository.GetResponseByIdAsync(
            teamId, cancellation);

        if (team is null || team.LeagueId != leagueId)
        {
            throw new NotFoundException(
                $"{role} fantasy team '{teamId}' was not found " +
                $"in league '{leagueId}'.");
        }
    }
}
