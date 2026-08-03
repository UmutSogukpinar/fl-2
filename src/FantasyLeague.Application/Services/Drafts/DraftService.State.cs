using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<DraftStateResponse> GetStateAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        var league = await leagueRepository.GetResponseByIdAsync(
            leagueId, cancellationToken)
            ?? throw new NotFoundException($"League '{leagueId}' was not found.");
        var picks = await draftRepository.GetPicksAsync(
            leagueId, cancellationToken);

        return CreateState(leagueId, league.Status, league.UpdatedAt, picks);
    }

    private static DraftStateResponse CreateState(
        Guid leagueId,
        LeagueStatus status,
        DateTime? draftStartedAtUtc,
        IReadOnlyList<DraftPickResponse> picks)
    {
        var completed = picks.Count(pick => pick.NbaPlayerId.HasValue);
        var currentPick = picks.FirstOrDefault(pick => !pick.NbaPlayerId.HasValue);
        var deadlineUtc = GetPickDeadlineUtc(
            currentPick, draftStartedAtUtc, picks);

        return new DraftStateResponse(
            leagueId,
            status,
            completed,
            picks.Count,
            currentPick,
            deadlineUtc,
            picks);
    }

    private static DateTime? GetPickDeadlineUtc(
        DraftPickResponse? currentPick,
        DateTime? draftStartedAtUtc,
        IReadOnlyList<DraftPickResponse> picks)
    {
        if (currentPick is null)
        {
            return null;
        }

        var currentPickStartedAtUtc = picks
            .Where(pick =>
                pick.OverallPick < currentPick.OverallPick &&
                pick.PickedAt.HasValue)
            .OrderByDescending(pick => pick.OverallPick)
            .Select(pick => pick.PickedAt)
            .FirstOrDefault() ?? draftStartedAtUtc;

        return currentPickStartedAtUtc?.Add(PickDuration);
    }
}
