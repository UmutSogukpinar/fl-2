using Microsoft.EntityFrameworkCore;

using FantasyLeague.Application.Common.Interfaces;
using FantasyLeague.Domain.Entities;
using FantasyLeague.Infrastructure.Context;

namespace FantasyLeague.Infrastructure.Repositories;

public sealed class NbaPlayerRepository(AppDbContext dbContext) : INbaPlayerRepository
{
    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<NbaPlayer>().AddRangeAsync(players, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<PlayerStats>()
            .Where(stats => stats.Season == season && nbaPlayerIds.Contains(stats.NbaPlayerId))
            .ToDictionaryAsync(stats => stats.NbaPlayerId, cancellationToken);
    }

    public Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<PlayerStats>().AddRangeAsync(playerStats, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
