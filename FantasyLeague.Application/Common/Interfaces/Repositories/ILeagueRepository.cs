using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueRepository
{
    Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<LeagueResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LeagueResponse?> GetResponseByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);
    Task<League?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(League league, CancellationToken cancellationToken);
    void Remove(League league);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
