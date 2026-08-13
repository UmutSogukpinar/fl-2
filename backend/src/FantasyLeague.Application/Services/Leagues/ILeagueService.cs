using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Leagues;

public interface ILeagueService
{
    Task<PagedResponse<LeagueResponse>> GetAsync(PaginationRequest request, LeagueStatus? status = null, CancellationToken cancellation = default);
    Task<LeagueResponse> GetByIdAsync(Guid id, CancellationToken cancellation = default);
    Task<LeagueResponse> CreateAsync(CreateLeagueRequest request, CancellationToken cancellation = default);
    Task<LeagueResponse> UpdateAsync(Guid id, UpdateLeagueRequest request, CancellationToken cancellation = default);
    Task DeleteAsync(Guid id, Guid commissionerId, CancellationToken cancellation = default);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid id, CancellationToken cancellation = default);
    Task<IReadOnlyList<LeagueStandingResponse>> GetStandingsAsync(Guid id, CancellationToken cancellation = default);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid id, CancellationToken cancellation = default);
    Task<MatchStats> GetMatchStatsAsync(Guid leagueId, Guid homeTeamId, Guid awayTeamId, CancellationToken cancellation = default);
    Task<int> ProcessDueFixturesAsync(DateTime utcNow, CancellationToken cancellation = default);
}
