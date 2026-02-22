using MACHTEN.Application.Contracts;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Infrastructure.Services;

public sealed class ExternalPricingService : IExternalPricingService
{
    // Simulated price cache -- in production this would be populated by a background
    // worker pulling from an external API or message queue.
    private static readonly Money CachedPrice = new(29.99m, "USD");

    public ValueTask<Money> GetLivePriceAsync(Guid productId, CancellationToken ct = default)
    {
        // Synchronous completion path: ValueTask avoids the Task allocation entirely.
        // Under load testing this means zero heap allocations per call.
        return ValueTask.FromResult(CachedPrice);
    }
}
