using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository
{
    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellation)
    {
        return await _dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellation);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellation)
    {
        return _dbContext.Set<NbaPlayer>().AddRangeAsync(
            players,
            cancellation);
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellation)
    {
        return await _dbContext.Set<PlayerStats>()
            .Where(stats =>
                stats.Season == season
                && nbaPlayerIds.Contains(stats.NbaPlayerId))
            .ToDictionaryAsync(stats => stats.NbaPlayerId, cancellation);
    }

    public Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellation)
    {
        return _dbContext.Set<PlayerStats>().AddRangeAsync(
            playerStats,
            cancellation);
    }

    public Task SaveChangesAsync(CancellationToken cancellation)
    {
        return _dbContext.SaveChangesAsync(cancellation);
    }
}
