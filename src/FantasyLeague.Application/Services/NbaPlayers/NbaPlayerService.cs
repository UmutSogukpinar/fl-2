using FantasyLeague.Application.Common.Exceptions;
using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Application.Common.Caching;
using FantasyLeague.Application.Common.Interfaces.Caching;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Responses.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.Common.Normalization;
using FantasyLeague.Application.Common.Validation;
using FantasyLeague.Application.Common.Pagination;


namespace FantasyLeague.Application.Services.NbaPlayers;

public sealed class NbaPlayerService(
    INbaPlayerRepository _nbaPlayerRepository,
    ICacheService _cacheService)
    : INbaPlayerService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

    public async Task<PagedResponse<NbaPlayerBasicResponse>> GetAsync(
        PaginationRequest req,
        CancellationToken cancellation = default)
    {
        req.ValidatePaginationRequest();

        var (items, totalCount) = await
            _nbaPlayerRepository.GetPagedAsync(
                req,
                cancellation
            );

        return Pagination.CreateResponse(items, totalCount, req);
    }

    public async Task<IPlayerResponse> GetNbaPlayerByIdAndYearAsync(
        Guid id,
        int season,
        PlayerResponseSize size,
        CancellationToken cancellation = default
    )
    {
        id.ValidatePlayerDetailsRequest(season, size);

        async Task<IPlayerResponse> GetPlayerAsync(CancellationToken token) =>
            await _nbaPlayerRepository.GetByIdAndSeasonAsync(
                id,
                season,
                size,
                token
            ) ?? throw new NotFoundException(
                    $"NBA player '{id}' was not found."
                );

        return await _cacheService.GetOrCreateAsync(
            GetCacheKey(id, season, size),
            GetPlayerAsync,
            CacheDuration,
            cancellation
        );
    }

    public async Task<PagedResponse<IPlayerResponse>>
    GetNbaPlayersByNameAndYearAsync(
        PaginationRequest pagReq,
        GetNbaPlayersRequest playerReq,
        CancellationToken cancellation = default
    )
    {
        pagReq.ValidatePaginationRequest();
        playerReq = playerReq.NormalizePlayerRequest();
        playerReq.ValidateNbaPlayerRequest();

        var (items, totalCount) = await
            _nbaPlayerRepository.GetPagedNbaPlayersByNameAsync(
                pagReq,
                playerReq,
                cancellation
            );

        return Pagination.CreateResponse(items, totalCount, pagReq);
    }

    private static string GetCacheKey(
        Guid id,
        int season,
        PlayerResponseSize size
    ) => size switch
    {
        PlayerResponseSize.Basic => CacheKeys.NbaPlayerBasic(id),
        PlayerResponseSize.Detailed => CacheKeys.NbaPlayerDetailed(id),
        PlayerResponseSize.Extended => CacheKeys.NbaPlayerExtended(
            id,
            season
        ),
        _ => throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                "Invalid player response size."
            )
    };
}
