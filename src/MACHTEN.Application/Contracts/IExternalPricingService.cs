using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Contracts;

public interface IExternalPricingService
{
    /// <summary>
    /// Returns live pricing for a product. Uses ValueTask to avoid Task allocation
    /// when the result is already available (e.g. served from cache).
    /// </summary>
    ValueTask<Money> GetLivePriceAsync(Guid productId, CancellationToken ct = default);
}
