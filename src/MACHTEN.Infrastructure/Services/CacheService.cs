using MACHTEN.Application.Contracts;
using Microsoft.Extensions.Caching.Hybrid;

namespace MACHTEN.Infrastructure.Services;

public sealed class CacheService(HybridCache cache) : ICacheService
{
    public ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken ct = default)
    {
        var options = (expiration, localExpiration) switch
        {
            (not null, not null) => new HybridCacheEntryOptions
            {
                Expiration = expiration.Value,
                LocalCacheExpiration = localExpiration.Value
            },
            (not null, null) => new HybridCacheEntryOptions { Expiration = expiration.Value },
            _ => null
        };

        return cache.GetOrCreateAsync(key, state, factory, options, cancellationToken: ct);
    }

    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var options = expiration.HasValue
            ? new HybridCacheEntryOptions { Expiration = expiration.Value }
            : null;

        return cache.GetOrCreateAsync(key, factory, options, cancellationToken: ct);
    }

    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        => cache.RemoveAsync(key, ct);
}
