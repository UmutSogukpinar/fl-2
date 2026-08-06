using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    public async Task<IReadOnlyList<DraftStateResponse>> StartDueDraftsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var leagues = await leagueRepository.GetDueForDraftAsync(
            utcNow, cancellationToken);
        if (leagues.Count == 0)
        {
            return [];
        }

        var startedLeagues = new List<League>();
        foreach (var league in leagues)
        {
            var teamIds = await teamRepository.GetIdsByLeagueIdAsync(
                league.Id, cancellationToken);
            if (teamIds.Count < MinimumTeamCount)
            {
                DelayDraft(league, utcNow);
                continue;
            }

            await EnsureDraftOrderExistsAsync(
                league, teamIds, cancellationToken);
            league.Status = LeagueStatus.Drafting;
            league.UpdatedAt = utcNow;
            startedLeagues.Add(league);
        }

        await leagueRepository.SaveChangesAsync(cancellationToken);
        return await CreateStatesAsync(startedLeagues, cancellationToken);
    }

    private static void DelayDraft(League league, DateTime utcNow)
    {
        if (league.Status == LeagueStatus.DraftDelayed)
        {
            return;
        }

        league.Status = LeagueStatus.DraftDelayed;
        league.UpdatedAt = utcNow;
    }

    private async Task EnsureDraftOrderExistsAsync(
        League league,
        IReadOnlyCollection<Guid> teamIds,
        CancellationToken cancellationToken)
    {
        if (await leagueSetupRepository.DraftOrderExistsAsync(
                league.Id, cancellationToken))
        {
            return;
        }

        var randomOrder = LeagueSetupGenerator.CreateRandomTeamOrder(teamIds);
        var draftOrder = LeagueSetupGenerator.CreateSnakeDraftOrder(
            league.Id,
            randomOrder,
            league.Settings.RosterSize
        );

        await leagueSetupRepository.AddDraftOrderAsync(
            draftOrder, cancellationToken);
    }

    private async Task<IReadOnlyList<DraftStateResponse>> CreateStatesAsync(
        IReadOnlyCollection<League> leagues,
        CancellationToken cancellationToken)
    {
        var states = new List<DraftStateResponse>(leagues.Count);
        foreach (var league in leagues)
        {
            var picks = await draftRepository.GetPicksAsync(
                league.Id, cancellationToken);
            states.Add(CreateState(
                league.Id, league.Status, league.UpdatedAt, picks));
        }

        return states;
    }
}
