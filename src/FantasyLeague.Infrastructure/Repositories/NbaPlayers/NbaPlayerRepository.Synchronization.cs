using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FantasyLeague.Infrastructure.Repositories.NbaPlayers;

public sealed partial class NbaPlayerRepository
{
    public async Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<NbaPlayer>()
            .Where(player => nbaIds.Contains(player.NbaId))
            .ToDictionaryAsync(player => player.NbaId, cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<NbaPlayer>().AddRangeAsync(
            players,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<PlayerStats>()
            .Where(stats =>
                stats.Season == season
                && nbaPlayerIds.Contains(stats.NbaPlayerId))
            .ToDictionaryAsync(stats => stats.NbaPlayerId, cancellationToken);
    }

    public Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<PlayerStats>().AddRangeAsync(
            playerStats,
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
