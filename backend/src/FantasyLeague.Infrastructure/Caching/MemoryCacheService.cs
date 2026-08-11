using FantasyLeague.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace FantasyLeague.Infrastructure.Caching;

public sealed class MemoryCacheService(
    IMemoryCache memoryCache
) : ICacheService
{
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> getFromRepo,
        TimeSpan expiration,
        CancellationToken cancellation)
    {
        if (memoryCache.TryGetValue<T>(key, out var cachedValue))
            return cachedValue!;

        var value = await getFromRepo(cancellation);
        memoryCache.Set(key, value, expiration);

        return value;
    }

    public void Remove(string key) => memoryCache.Remove(key);
}
