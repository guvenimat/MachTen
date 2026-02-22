namespace MACHTEN.Api.Infrastructure.Logging;

public static partial class LoggerMessageDefinitions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Product fetched: {ProductId}")]
    public static partial void LogProductFetched(this ILogger logger, Guid productId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Products listed: {Count} items returned")]
    public static partial void LogProductsListed(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Product created: {ProductId}")]
    public static partial void LogProductCreated(this ILogger logger, Guid productId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Product not found: {ProductId}")]
    public static partial void LogProductNotFound(this ILogger logger, Guid productId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing request")]
    public static partial void LogUnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded for policy: {PolicyName}")]
    public static partial void LogRateLimitExceeded(this ILogger logger, string policyName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache hit for key: {CacheKey}")]
    public static partial void LogCacheHit(this ILogger logger, string cacheKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache miss for key: {CacheKey}")]
    public static partial void LogCacheMiss(this ILogger logger, string cacheKey);
}
