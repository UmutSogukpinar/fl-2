namespace FantasyLeague.Application.Common.Interfaces.Caching;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellation = default);

    void Remove(string key);
}
