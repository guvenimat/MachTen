using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Caching.Distributed;

namespace MACHTEN.Api.Infrastructure.Caching;

public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly DistributedCacheEntryOptions DefaultOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null || bytes.Length == 0)
            return null;

        return JsonSerializer.Deserialize<T>(bytes, GetJsonTypeInfo<T>());
    }

    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var options = expiry.HasValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry.Value }
            : DefaultOptions;

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, GetJsonTypeInfo<T>());
        await cache.SetAsync(key, bytes, options, ct);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
    }

    private static JsonTypeInfo<T> GetJsonTypeInfo<T>()
    {
        return (JsonTypeInfo<T>?)AppSerializerContext.Default.GetTypeInfo(typeof(T))
            ?? throw new InvalidOperationException(
                $"Type {typeof(T).Name} is not registered in AppSerializerContext. " +
                $"Add [JsonSerializable(typeof({typeof(T).Name}))] to the context.");
    }
}
