using FantasyLeague.Application.Common.Interfaces.Repositories;
using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;
using FantasyLeague.Infrastructure.Repositories.Projections;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class NbaPlayerRepository(AppDbContext dbContext) : INbaPlayerRepository
{
    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken
    ){
        return await dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken
    ){
        return dbContext.Set<NbaPlayer>().AddRangeAsync(
            players, cancellationToken
        );
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken
    ){
        return await dbContext.Set<PlayerStats>()
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
        return dbContext.Set<PlayerStats>().AddRangeAsync(
            playerStats, cancellationToken
        );
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
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


    private Task<NbaPlayerBasicResponse?> GetBasicAsync(
       Guid id,
       CancellationToken cancelllation
    ){
        return dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(NbaPlayerProjections.Basic)
            .SingleOrDefaultAsync(cancelllation);
    }

    private Task<NbaPlayerDetailedResponse?> GetDetailedAsync(
        Guid id,
        CancellationToken cancellation
    ){
        return dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(NbaPlayerProjections.Detailed)
            .SingleOrDefaultAsync(cancellation);
    }

    private Task<NbaPlayerExtendedResponse?> GetExtendedAsync(
        Guid id,
        int season,
        CancellationToken cancellation
    ){
        return dbContext.Set<NbaPlayer>()
            .AsNoTracking()
            .Where(player => player.Id == id)
            .Select(NbaPlayerProjections.Extended(season))
            .SingleOrDefaultAsync(cancellation);
    }
}
