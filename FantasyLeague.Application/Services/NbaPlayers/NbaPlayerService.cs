using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Common.Caching;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;

namespace FantasyLeague.Application.Services.NbaPlayers;

public sealed class NbaPlayerService(
    INbaPlayerRepository nbaPlayerRepository,
    ICacheService cacheService)
    : INbaPlayerService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

    public async Task<PagedResponse<NbaPlayerBasicResponse>> GetAsync(
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await nbaPlayerRepository.GetPagedAsync(
            request.PageNumber, request.PageSize, cancellationToken);

        return new PagedResponse<NbaPlayerBasicResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize));
    }

    public async Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(
        Guid id,
        int season,
        PlayerResponseSize size,
        CancellationToken cancellation
    )
    {
        async Task<IPlayerResponse> GetPlayerAsync(CancellationToken token) =>
            await nbaPlayerRepository.GetByIdAndSeasonAsync(
                id,
                season,
                size,
                token
            ) ?? throw new NotFoundException($"NBA player '{id}' was not found.");

        return await cacheService.GetOrCreateAsync(
            GetCacheKey(id, season, size),
            GetPlayerAsync,
            CacheDuration,
            cancellation
        );
    }

    private static string GetCacheKey(Guid id, int season, PlayerResponseSize size) => size switch
    {
        PlayerResponseSize.Basic => CacheKeys.NbaPlayerBasic(id),
        PlayerResponseSize.Detailed => CacheKeys.NbaPlayerDetailed(id),
        PlayerResponseSize.Extended => CacheKeys.NbaPlayerExtended(id, season),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, "Invalid player response size.")
    };
}
