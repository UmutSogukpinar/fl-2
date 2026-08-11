using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Drafts;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<IReadOnlyList<DraftStateResponse>> AutoPickExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellation = default)
    {
        var leagues = await leagueRepository.GetDraftingAsync(cancellation);
        var updatedStates = new List<DraftStateResponse>();

        foreach (var league in leagues)
        {
            var state = await TryAutoPickAsync(
                league, utcNow, cancellation);
            if (state is not null)
            {
                updatedStates.Add(state);
            }
        }

        return updatedStates;
    }

    private async Task<DraftStateResponse?> TryAutoPickAsync(
        League league,
        DateTime utcNow,
        CancellationToken cancellation)
    {
        var picks = await draftRepository.GetPicksAsync(
            league.Id, cancellation);
        var currentPick = picks.FirstOrDefault(
            pick => !pick.NbaPlayerId.HasValue);
        var deadlineUtc = GetPickDeadlineUtc(
            currentPick, league.UpdatedAt, picks);

        if (currentPick is null || deadlineUtc is null || deadlineUtc > utcNow)
        {
            return null;
        }

        var trackedPick = await draftRepository.GetCurrentTrackedPickAsync(
            league.Id, cancellation);
        var nbaPlayerId = await draftRepository.GetFirstAvailablePlayerIdAsync(
            league.Id, cancellation);
        if (trackedPick is null ||
            trackedPick.Id != currentPick.Id ||
            nbaPlayerId is null)
        {
            return null;
        }

        await ApplyPickAsync(
            league.Id,
            trackedPick,
            nbaPlayerId.Value,
            utcNow,
            cancellation);
        await CompleteDraftIfFinalPickAsync(
            league,
            trackedPick,
            picks,
            utcNow,
            cancellation);

        ResetFailureCount(league);

        if (!await draftRepository.TrySaveChangesAsync(cancellation))
        {
            return await GetCancellationStateAfterFailureAsync(
                league.Id,
                utcNow,
                cancellation);
        }

        var updatedPicks = await draftRepository.GetPicksAsync(
            league.Id, cancellation);
        return CreateState(
            league.Id, league.Status, league.UpdatedAt, updatedPicks);
    }
}
