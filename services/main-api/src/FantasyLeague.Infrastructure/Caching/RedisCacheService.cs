using System.Text.Json;

using FantasyLeague.Application.Common.Interfaces.Caching;

using Microsoft.Extensions.Caching.Distributed;

namespace FantasyLeague.Infrastructure.Caching;

public sealed class RedisCacheService(
    IDistributedCache distributedCache
) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> getFromRepo,
        TimeSpan expiration,
        CancellationToken cancellation)
    {
        var cachedValue = await distributedCache.GetAsync(key, cancellation);
        if (cachedValue is not null)
            return JsonSerializer.Deserialize<T>(cachedValue, SerializerOptions)!;

        var value = await getFromRepo(cancellation);
        var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

        await distributedCache.SetAsync(
            key,
            serializedValue,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            },
            cancellation);

        return value;
    }

    public void Remove(string key) => distributedCache.Remove(key);
}
