using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<DraftStateResponse> CloseDelayedLeagueAsync(
        Guid leagueId,
        Guid commissionerId,
        CancellationToken cancellation = default)
    {
        var league = await leagueRepository.GetTrackedByIdAsync(
            leagueId, cancellation)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");

        EnsureCommissionerCanClose(league, commissionerId);
        EnsureLeagueIsDelayed(league);

        league.Status = LeagueStatus.Completed;
        league.UpdatedAt = DateTime.UtcNow;
        await leagueRepository.SaveChangesAsync(cancellation);

        var picks = await draftRepository.GetPicksAsync(
            leagueId, cancellation);
        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    private static void EnsureCommissionerCanClose(
        League league,
        Guid commissionerId)
    {
        if (league.CommissionerId != commissionerId)
        {
            throw new ForbiddenException(
                "Only the league commissioner can close the league.");
        }
    }

    private static void EnsureLeagueIsDelayed(League league)
    {
        if (league.Status != LeagueStatus.DraftDelayed)
        {
            throw new ConflictException(
                "Only a delayed league can be closed.");
        }
    }
}
