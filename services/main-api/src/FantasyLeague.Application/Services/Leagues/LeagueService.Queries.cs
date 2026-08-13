using FantasyLeague.Application.Common.Pagination;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Responses.Leagues;
using FantasyLeague.Domain.Enums;

namespace FantasyLeague.Application.Services.Leagues;

public sealed partial class LeagueService
{
    public async Task<PagedResponse<LeagueResponse>> GetAsync(
        PaginationRequest request,
        LeagueStatus? status = null,
        CancellationToken cancellation = default)
    {
        request.ValidatePaginationRequest();

        var (items, totalCount) = await _leagueRepository.GetPagedAsync(
            request,
            status,
            cancellation);

        return items.CreateResponse(totalCount, request);
    }

    public Task<LeagueResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellation = default)
    {
        return GetLeagueOrThrowAsync(id, cancellation);
    }
}
