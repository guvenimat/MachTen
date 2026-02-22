using Microsoft.Extensions.Caching.Hybrid;

namespace MACHTEN.Api.Infrastructure.Caching;

/// <summary>
/// High-performance cache-aside service backed by HybridCache (L1 in-process + L2 distributed).
/// HybridCache internally uses stampede protection -- only one factory call executes per key
/// even under high concurrency, preventing thundering-herd on cache misses.
///
/// Performance notes for large payloads:
/// - HybridCache uses pooled buffers internally (via IBufferDistributedCache) to avoid
///   Large Object Heap (LOH) allocations for payloads > 85KB.
/// - When Garnet/Redis is configured as the L2 backing store and implements
///   IBufferDistributedCache, serialization writes directly into rented ArrayPool&lt;byte&gt;
///   buffers instead of allocating new byte[] arrays.
/// - The L1 in-memory tier avoids serialization entirely for repeat reads within
///   LocalCacheExpiration, yielding sub-microsecond access times.
/// </summary>
public sealed class MachtenCacheService(HybridCache cache)
{
    /// <summary>
    /// Gets a value from L1/L2 cache, or creates it via the factory delegate on miss.
    /// The factory receives <paramref name="state"/> to avoid closure allocations.
    /// </summary>
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

    /// <summary>
    /// Gets a value from L1/L2 cache, or creates it via a stateless factory delegate.
    /// Prefer the <typeparamref name="TState"/> overload to avoid closure allocations.
    /// </summary>
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
