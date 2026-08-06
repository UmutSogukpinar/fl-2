using FantasyLeague.Domain.Entities.Drafts;
using FantasyLeague.Domain.Entities.FantasyTeams;

using FantasyLeague.Application.DTOs.Responses.Drafts;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface IDraftRepository
{
    Task<IReadOnlyList<DraftPickResponse>> GetPicksAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<DraftPickOrder?> GetCurrentTrackedPickAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<bool> IsPlayerUnavailableAsync(Guid leagueId, Guid nbaPlayerId, CancellationToken cancellationToken);
    Task<bool> NbaPlayerExistsAsync(Guid nbaPlayerId, CancellationToken cancellationToken);
    Task<Guid?> GetFirstAvailablePlayerIdAsync(Guid leagueId, CancellationToken cancellationToken);
    Task<FantasyTeam?> GetTeamAsync(Guid leagueId, Guid teamId, CancellationToken cancellationToken);
    Task AddRosterPlayerAsync(FantasyTeamPlayer player, CancellationToken cancellationToken);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}
