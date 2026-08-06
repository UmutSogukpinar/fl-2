using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    private async Task CompleteDraftIfFinalPickAsync(
        League league,
        DraftPickOrder currentPick,
        IReadOnlyList<DraftPickResponse> picks,
        DateTime pickedAt,
        CancellationToken cancellationToken)
    {
        if (currentPick.OverallPick != picks.Count)
        {
            return;
        }

        await CreateFixturesAsync(league.Id, picks, pickedAt, cancellationToken);
        league.Status = LeagueStatus.Active;
        league.UpdatedAt = pickedAt;
    }

    private Task CreateFixturesAsync(
        Guid leagueId,
        IReadOnlyList<DraftPickResponse> picks,
        DateTime draftCompletedAt,
        CancellationToken cancellationToken)
    {
        var teamOrder = picks
            .Where(pick => pick.Round == 1)
            .OrderBy(pick => pick.PositionInRound)
            .Select(pick => pick.TeamId)
            .ToArray();
        var fixtures = LeagueSetupGenerator.CreateRoundRobinFixtures(
            leagueId, teamOrder, draftCompletedAt, TimeSpan.FromMinutes(5));

        return leagueSetupRepository.AddFixturesAsync(
            fixtures, cancellationToken
        );
    }
}
