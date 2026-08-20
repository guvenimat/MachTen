using MACHTEN.Api.Infrastructure.Logging;
using TickerQ.Utilities.Base;

namespace MACHTEN.Api.Jobs;

/// <summary>
/// Sample recurring job. TickerQ discovers <see cref="TickerFunctionAttribute"/>
/// methods with a source generator, so there is no reflection scan at startup.
/// </summary>
public sealed class HeartbeatJob(ILogger<HeartbeatJob> logger)
{
    [TickerFunction(functionName: nameof(Heartbeat), cronExpression: "*/5 * * * *")]
    public Task Heartbeat(CancellationToken ct)
    {
        logger.LogHeartbeat(DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
