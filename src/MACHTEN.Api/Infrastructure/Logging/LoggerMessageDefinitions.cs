using ZLogger;

namespace MACHTEN.Api.Infrastructure.Logging;

/// <summary>
/// Log messages generated at compile time by ZLogger's source generator.
/// Nothing is formatted unless the level is enabled, and the generated code
/// writes UTF8 straight to the sink rather than building an intermediate string.
/// </summary>
public static partial class LoggerMessageDefinitions
{
    [ZLoggerMessage(LogLevel.Error, "Unhandled exception processing request")]
    public static partial void LogUnhandledException(this ILogger logger, Exception exception);

    [ZLoggerMessage(LogLevel.Warning, "Rate limit exceeded for policy: {policyName}")]
    public static partial void LogRateLimitExceeded(this ILogger logger, string policyName);
}
