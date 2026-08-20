using System.Diagnostics;

namespace MACHTEN.Api.Infrastructure.Observability;

/// <summary>
/// Ties every log line and trace for a request to one id. Honours an inbound
/// X-Correlation-ID so a call crossing several services keeps the same thread
/// of evidence, and always echoes it back on the response.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var inbound)
                            && !string.IsNullOrWhiteSpace(inbound)
            ? inbound.ToString()
            : Activity.Current?.TraceId.ToString() ?? Guid.CreateVersion7().ToString();

        context.Response.Headers[HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        // Scope, not a log call: every entry written downstream carries the id.
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
