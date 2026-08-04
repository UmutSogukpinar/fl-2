using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueRepository
{
    Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)> GetPagedAsync(
        PaginationRequest request, LeagueStatus? status, CancellationToken cancellationToken);
    Task<LeagueResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LeagueResponse?> GetResponseByJoinCodeAsync(string joinCode, CancellationToken cancellationToken);
    Task<League?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<League>> GetDueForDraftAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<League>> GetDraftingAsync(CancellationToken cancellationToken);
    Task AddAsync(League league, CancellationToken cancellationToken);
    void Remove(League league);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
