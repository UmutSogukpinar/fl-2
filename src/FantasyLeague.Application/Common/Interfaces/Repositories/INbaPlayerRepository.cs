using FantasyLeague.Domain.Entities.Players;

using FantasyLeague.Application.DTOs.Responses.NbaPlayers;
using FantasyLeague.Application.DTOs.Requests.Common;
using FantasyLeague.Application.DTOs.Requests.NbaPlayers;
using FantasyLeague.Application.Models;
using FantasyLeague.Domain.Entities;

namespace FantasyLeague.Application.Common.Interfaces.Repositories;

public interface INbaPlayerRepository
{
    Task<(IReadOnlyCollection<NbaPlayerBasicResponse> Items, int TotalCount)> GetPagedAsync(PaginationRequest request, CancellationToken cancellation);
    Task<IReadOnlyDictionary<int, NbaPlayer>> GetByNbaIdsAsync(IReadOnlyCollection<int> nbaIds, CancellationToken cancellation);
    Task AddRangeAsync(IEnumerable<NbaPlayer> players, CancellationToken cancellation);
    Task<IReadOnlyDictionary<Guid, PlayerStats>> GetPlayerStatsAsync(IReadOnlyCollection<Guid> nbaPlayerIds, int season, CancellationToken cancellation);
    Task AddStatsRangeAsync(IEnumerable<PlayerStats> playerStats, CancellationToken cancellation);
    Task SaveChangesAsync(CancellationToken cancellation);
    Task<IPlayerResponse?> GetByIdAndSeasonAsync(Guid id, int season, PlayerResponseSize size, CancellationToken cancellation);
    Task<(IReadOnlyCollection<IPlayerResponse> Items, int TotalCount)> GetPagedNbaPlayersByNameAsync(PaginationRequest pagination, GetNbaPlayersRequest request, CancellationToken cancellation);

    Task<MatchStats> GetMatchStatsByTeamIdsAsync(
        Guid leagueId,
        Guid homeTeamId,
        Guid awayTeamId,
        int season,
        CancellationToken cancellation);

}
