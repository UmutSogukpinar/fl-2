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
        CancellationToken cancellationToken = default)
    {
        request.ValidatePaginationRequest();

        var (items, totalCount) = await _leagueRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            status,
            cancellationToken);

        return new PagedResponse<LeagueResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalCount.CalculateTotalPage(request.PageSize));
    }

    public Task<LeagueResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return GetLeagueOrThrowAsync(id, cancellationToken);
    }
}
