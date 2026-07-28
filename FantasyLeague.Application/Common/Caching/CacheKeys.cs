namespace FantasyLeague.Application.Common.Caching;

public static class CacheKeys
{
    public static string NbaPlayerBasic(Guid playerId) => $"nba-player:{playerId:N}:basic";
    public static string NbaPlayerDetailed(Guid playerId) => $"nba-player:{playerId:N}:detailed";
    public static string NbaPlayerExtended(Guid playerId, int season) =>
        $"nba-player:{playerId:N}:extended:{season}";
}
