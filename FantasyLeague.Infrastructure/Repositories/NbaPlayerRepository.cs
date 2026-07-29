using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.Repositories.Projections;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class NbaPlayerRepository(AppDbContext _dbContext) : INbaPlayerRepository
{
    public async Task<(IReadOnlyCollection<NbaPlayerBasicResponse> Items,
        int TotalCount)>
    GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<NbaPlayer>().AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToBasic()
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyCollection<IPlayerResponse> Items,
        int TotalCount)>
    GetPagedNbaPlayersByNameAsync(
        PaginationRequest pageReq,
        GetNbaPlayersRequest playerReq,
        CancellationToken cancellation
    )
    {
        var query = _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player =>
                (playerReq.Id == Guid.Empty || player.Id == playerReq.Id) &&
                (playerReq.Name == string.Empty ||
                    player.FirstName.ToLower().Contains(playerReq.Name)) &&
                (playerReq.Surname == string.Empty ||
                    player.LastName.ToLower().Contains(playerReq.Surname)));

        var totalCount = await query.CountAsync(cancellation);
        query = query
            .OrderBy(player => player.FirstName)
            .ThenBy(player => player.LastName)
            .ThenBy(player => player.Id)
            .Skip((pageReq.PageNumber - 1) * pageReq.PageSize)
            .Take(pageReq.PageSize);

        IReadOnlyCollection<IPlayerResponse> items = playerReq.Size switch
        {
            PlayerResponseSize.Basic => await query.ToBasic()
                .ToArrayAsync(cancellation),
            PlayerResponseSize.Detailed => await query.ToDetailed()
                .ToArrayAsync(cancellation),
            PlayerResponseSize.Extended => await query
                .ToExtended(playerReq.Season)
                .ToArrayAsync(cancellation),
            _ => throw new ArgumentOutOfRangeException(
                nameof(playerReq.Size),
                playerReq.Size,
                "Invalid player response size.")
        };

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken
    )
    {
        return _dbContext.Set<NbaPlayer>().AddRangeAsync(
            players,
            cancellationToken
        );
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken
    ){
        return await _dbContext.Set<PlayerStats>()
            .Where(
                stats => stats.Season == season && 
                nbaPlayerIds.Contains(stats.NbaPlayerId)
            )
            .ToDictionaryAsync(stats => stats.NbaPlayerId, cancellationToken);
    }

    public Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<PlayerStats>().AddRangeAsync(
            playerStats, cancellationToken
        );
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IPlayerResponse?> GetByIdAndSeasonAsync(
        Guid id,
        int season, 
        PlayerResponseSize size,
        CancellationToken cancellationToken
    ){
        return size switch
        {
            PlayerResponseSize.Basic => await GetBasicAsync(
                                            id,
                                            cancellationToken
                                        ),
            
            PlayerResponseSize.Detailed => await GetDetailedAsync(
                                                id,
                                                cancellationToken),

            PlayerResponseSize.Extended => await GetExtendedAsync(
                                                id,
                                                season,
                                                cancellationToken
                                           ),

            _ => throw new ArgumentOutOfRangeException(
                    nameof(size), size, "Invalid player response size."),
        };
    }

    // ====================== Utils of GetByIdAndSeasonAsync() ======================

    private Task<NbaPlayerBasicResponse?> GetBasicAsync(
       Guid id,
       CancellationToken cancelllation
    ){
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToBasic()
            .SingleOrDefaultAsync(cancelllation);
    }

    private Task<NbaPlayerDetailedResponse?> GetDetailedAsync(
        Guid id,
        CancellationToken cancellation
    ){
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToDetailed()
            .SingleOrDefaultAsync(cancellation);
    }

    private Task<NbaPlayerExtendedResponse?> GetExtendedAsync(
        Guid id,
        int season,
        CancellationToken cancellation
    ){
        return _dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player => player.Id == id)
            .ToExtended(season)
            .SingleOrDefaultAsync(cancellation);
    }
}
