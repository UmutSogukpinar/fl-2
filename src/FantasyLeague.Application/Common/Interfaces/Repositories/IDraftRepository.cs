using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IDraftRepository
{
    Task<IReadOnlyList<DraftPickResponse>> GetPicksAsync(Guid leagueId, CancellationToken cancellation);
    Task<DraftPickOrder?> GetCurrentTrackedPickAsync(Guid leagueId, CancellationToken cancellation);
    Task<bool> IsPlayerUnavailableAsync(Guid leagueId, Guid nbaPlayerId, CancellationToken cancellation);
    Task<bool> NbaPlayerExistsAsync(Guid nbaPlayerId, CancellationToken cancellation);
    Task<Guid?> GetFirstAvailablePlayerIdAsync(Guid leagueId, CancellationToken cancellation);
    Task<FantasyTeam?> GetTeamAsync(Guid leagueId, Guid teamId, CancellationToken cancellation);
    Task AddRosterPlayerAsync(FantasyTeamPlayer player, CancellationToken cancellation);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellation);
}
