using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<IReadOnlyList<DraftStateResponse>> AutoPickExpiredAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var leagues = await leagueRepository.GetDraftingAsync(cancellationToken);
        var updatedStates = new List<DraftStateResponse>();

        foreach (var league in leagues)
        {
            var state = await TryAutoPickAsync(
                league, utcNow, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        var picks = await draftRepository.GetPicksAsync(
            league.Id, cancellationToken);
        var currentPick = picks.FirstOrDefault(
            pick => !pick.NbaPlayerId.HasValue);
        var deadlineUtc = GetPickDeadlineUtc(
            currentPick, league.UpdatedAt, picks);

        if (currentPick is null || deadlineUtc is null || deadlineUtc > utcNow)
        {
            return null;
        }

        var trackedPick = await draftRepository.GetCurrentTrackedPickAsync(
            league.Id, cancellationToken);
        var nbaPlayerId = await draftRepository.GetFirstAvailablePlayerIdAsync(
            league.Id, cancellationToken);
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
            cancellationToken);
        await CompleteDraftIfFinalPickAsync(
            league,
            trackedPick,
            picks,
            utcNow,
            cancellationToken);

        if (!await draftRepository.TrySaveChangesAsync(cancellationToken))
        {
            return null;
        }

        var updatedPicks = await draftRepository.GetPicksAsync(
            league.Id, cancellationToken);
        return CreateState(
            league.Id, league.Status, league.UpdatedAt, updatedPicks);
    }

    private async Task ApplyPickAsync(
        Guid leagueId,
        DraftPickOrder pick,
        Guid nbaPlayerId,
        DateTime pickedAt,
        CancellationToken cancellationToken)
    {
        pick.NbaPlayerId = nbaPlayerId;
        pick.PickedAt = pickedAt;
        await draftRepository.AddRosterPlayerAsync(new FantasyTeamPlayer
        {
            LeagueId = leagueId,
            FantasyTeamId = pick.TeamId,
            NbaPlayerId = nbaPlayerId,
            AcquiredAt = pickedAt
        }, cancellationToken);
    }
}
