using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Contracts;

public interface IPricingProvider
{
    ValueTask<Money> GetLivePriceAsync(Guid productId, CancellationToken ct = default);
}
