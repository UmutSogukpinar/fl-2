using FantasyLeague.Domain.Entities.Leagues;

using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Domain.Enums;
using FantasyLeague.Application.DTOs.Requests.Common;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface ILeagueRepository
{
    Task<(IReadOnlyCollection<LeagueResponse> Items, int TotalCount)> GetPagedAsync(
        PaginationRequest request, LeagueStatus? status, CancellationToken cancellation);
    Task<LeagueResponse?> GetResponseByIdAsync(Guid id, CancellationToken cancellation);
    Task<LeagueResponse?> GetResponseByJoinCodeAsync(string joinCode, CancellationToken cancellation);
    Task<League?> GetTrackedByIdAsync(Guid id, CancellationToken cancellation);
    Task<IReadOnlyList<League>> GetDueForDraftAsync(DateTime utcNow, CancellationToken cancellation);
    Task<IReadOnlyList<League>> GetDraftingAsync(CancellationToken cancellation);
    Task<bool> RecordDraftFailureAsync(Guid leagueId, int cancellationThreshold, DateTime utcNow, CancellationToken cancellation);
    Task AddAsync(League league, CancellationToken cancellation);
    void Remove(League league);
    Task SaveChangesAsync(CancellationToken cancellation);
}
