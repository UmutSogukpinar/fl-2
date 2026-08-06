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
        CancellationToken cancellationToken = default)
    {
        if (homeTeamId == awayTeamId)
        {
            throw new BadRequestException(
                "Home and away teams must be different."
            );
        }

        var league = await _leagueRepository.GetResponseByIdAsync(
            leagueId, cancellationToken)
            ?? throw new NotFoundException(
                    $"League '{leagueId}' was not found."
                );

        await EnsureTeamBelongsToLeagueAsync(
            homeTeamId, leagueId, "Home", cancellationToken
        );
        await EnsureTeamBelongsToLeagueAsync(
            awayTeamId, leagueId, "Away", cancellationToken
        );

        return await _playerRepository.GetMatchStatsByTeamIdsAsync(
            leagueId,
            homeTeamId,
            awayTeamId,
            league.Season,
            cancellationToken
        );
    }

    private async Task EnsureTeamBelongsToLeagueAsync(
        Guid teamId,
        Guid leagueId,
        string role,
        CancellationToken cancellationToken)
    {
        var team = await _teamRepository.GetResponseByIdAsync(
            teamId, cancellationToken);

        if (team is null || team.LeagueId != leagueId)
        {
            throw new NotFoundException(
                $"{role} fantasy team '{teamId}' was not found " +
                $"in league '{leagueId}'.");
        }
    }
}
