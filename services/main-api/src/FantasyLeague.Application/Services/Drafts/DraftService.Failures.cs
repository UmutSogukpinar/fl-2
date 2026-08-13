using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    private static void ResetFailureCount(League league)
    {
        league.ConsecutiveDraftFailureCount = 0;
    }

    private async Task<DraftStateResponse?> GetCancellationStateAfterFailureAsync(
        Guid leagueId,
        DateTime failedAt,
        CancellationToken cancellation)
    {
        var cancelled = await leagueRepository.RecordDraftFailureAsync(
            leagueId,
            DraftFailureCancellationThreshold,
            failedAt,
            cancellation);
        await leagueRepository.SaveChangesAsync(cancellation);

        if (!cancelled)
        {
            return null;
        }

        var picks = await draftRepository.GetPicksAsync(
            leagueId,
            cancellation);

        return CreateState(
            leagueId,
            LeagueStatus.DraftCancelled,
            failedAt,
            picks);
    }
}
