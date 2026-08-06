using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;

namespace FantasyLeague.Application.Services.Drafts;

public sealed partial class DraftService
{
    private async Task ApplyPickAsync(
        Guid leagueId,
        DraftPickOrder pick,
        Guid nbaPlayerId,
        DateTime pickedAt,
        CancellationToken cancellationToken)
    {
        pick.NbaPlayerId = nbaPlayerId;
        pick.PickedAt = pickedAt;

        var rosterPlayer = new FantasyTeamPlayer
        {
            LeagueId = leagueId,
            FantasyTeamId = pick.TeamId,
            NbaPlayerId = nbaPlayerId,
            AcquiredAt = pickedAt
        };

        await draftRepository.AddRosterPlayerAsync(
            rosterPlayer,
            cancellationToken);
    }
}
