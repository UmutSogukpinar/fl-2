using FantasyLeague.Application.DTOs.Requests.Leagues;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;

namespace FantasyLeague.Application.Services.Leagues;

public interface ILeagueService
{
    Task<PagedResponse<LeagueResponse>> GetAsync(PaginationRequest request, CancellationToken cancellationToken = default);
    Task<LeagueResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeagueResponse> CreateAsync(CreateLeagueRequest request, CancellationToken cancellationToken = default);
    Task<LeagueResponse> UpdateAsync(Guid id, UpdateLeagueRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid commissionerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeagueFixtureResponse>> GetFixturesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DraftPickOrderResponse>> GetDraftOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
