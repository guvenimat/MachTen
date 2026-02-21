namespace MACHTEN.Api.Infrastructure.Caching;

public interface ICacheService
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    ValueTask RemoveAsync(string key, CancellationToken ct = default);
}
