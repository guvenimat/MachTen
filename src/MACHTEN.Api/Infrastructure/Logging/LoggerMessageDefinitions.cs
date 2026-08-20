namespace MACHTEN.Api.Infrastructure.Logging;

public static partial class LoggerMessageDefinitions
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception processing request")]
    public static partial void LogUnhandledException(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded for policy: {PolicyName}")]
    public static partial void LogRateLimitExceeded(this ILogger logger, string policyName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache hit for key: {CacheKey}")]
    public static partial void LogCacheHit(this ILogger logger, string cacheKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache miss for key: {CacheKey}")]
    public static partial void LogCacheMiss(this ILogger logger, string cacheKey);

    [LoggerMessage(Level = LogLevel.Information, Message = "Heartbeat job ran at {RanAtUtc}")]
    public static partial void LogHeartbeat(this ILogger logger, DateTimeOffset ranAtUtc);
}
