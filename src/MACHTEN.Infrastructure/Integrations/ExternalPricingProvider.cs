using MACHTEN.Application.Contracts;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Infrastructure.Integrations;

public sealed class ExternalPricingProvider : IPricingProvider
{
    private static readonly Money CachedPrice = new(29.99m, "USD");

    public ValueTask<Money> GetLivePriceAsync(Guid productId, CancellationToken ct = default)
    {
        return ValueTask.FromResult(CachedPrice);
    }
}
