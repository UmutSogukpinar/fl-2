using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces;

public interface IFantasyTeamRepository
{
    Task<IReadOnlyCollection<FantasyTeam>> GetByLeagueIdAsync(
        Guid leagueId,
        CancellationToken cancellationToken);

    Task<FantasyTeam?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid leagueId,
        Guid ownerId,
        string name,
        Guid? excludedTeamId,
        CancellationToken cancellationToken);

    Task AddAsync(FantasyTeam team, CancellationToken cancellationToken);

    void Remove(FantasyTeam team);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
