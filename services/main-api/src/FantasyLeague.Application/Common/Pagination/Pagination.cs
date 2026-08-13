using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;

namespace FantasyLeague.Application.Common.Pagination;

public static class Pagination
{
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationRequest request)
    {
        return query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);
    }

    public static PagedResponse<T> CreateResponse<T>(
        this IReadOnlyCollection<T> items,
        int totalCount,
        PaginationRequest request)
    {
        return new PagedResponse<T>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            CalculateTotalPages(totalCount, request.PageSize));
    }

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        return (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
