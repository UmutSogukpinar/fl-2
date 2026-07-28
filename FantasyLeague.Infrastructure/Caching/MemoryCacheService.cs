using FantasyLeague.Application.Common.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace FantasyLeague.Infrastructure.Caching;

public sealed class MemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue<T>(key, out var cachedValue))
            return cachedValue!;

        var value = await factory(cancellationToken);
        memoryCache.Set(key, value, expiration);

        return value;
    }

    public void Remove(string key) => memoryCache.Remove(key);
}
