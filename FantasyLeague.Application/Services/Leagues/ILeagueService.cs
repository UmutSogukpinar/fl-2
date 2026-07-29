using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Leagues;

public interface ILeagueService
{
    Task<PagedResponse<LeagueResponse>> GetAsync(PaginationRequest request, LeagueStatus? status = null, CancellationToken cancellationToken = default);
    Task<LeagueResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeagueResponse> CreateAsync(CreateLeagueRequest request, CancellationToken cancellationToken = default);
    Task<LeagueResponse> UpdateAsync(Guid id, UpdateLeagueRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid commissionerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MatchStats> GetMatchStatsAsync(Guid leagueId, Guid homeTeamId, Guid awayTeamId, CancellationToken cancellationToken = default);
    Task<int> ProcessDueFixturesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
