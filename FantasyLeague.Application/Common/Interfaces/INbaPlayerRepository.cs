using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces;

public interface INbaPlayerRepository
{
    Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(
        IReadOnlyCollection<int> nbaIds,
        CancellationToken cancellationToken);

    Task AddRangeAsync(
        IEnumerable<NbaPlayer> players,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(
        IReadOnlyCollection<Guid> nbaPlayerIds,
        int season,
        CancellationToken cancellationToken);

    Task AddStatsRangeAsync(
        IEnumerable<PlayerStats> playerStats,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<NbaPlayer?> GetByIdAndSeasonAsync(
        Guid id,
        int season,
        PlayerResponseSize size,
        CancellationToken cancellationToken
    );
}
