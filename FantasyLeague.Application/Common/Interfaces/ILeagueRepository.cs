using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces;

public interface ILeagueRepository
{
    Task<IReadOnlyCollection<League>> GetAllAsync(CancellationToken cancellationToken);

    Task<League?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(League league, CancellationToken cancellationToken);

    void Remove(League league);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
