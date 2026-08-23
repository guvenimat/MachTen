using MACHTEN.Application.Contracts;
using MACHTEN.Application.Contracts.Persistence;

namespace MACHTEN.Application.Features.Orders.GetOrder;

public static class GetOrderHandler
{
    /// <summary>Long enough to absorb a burst, short enough that staleness stays boring.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public static ValueTask<OrderDto?> HandleAsync(
        GetOrderQuery query,
        IOrderRepository orders,
        ICacheStore cache,
        CancellationToken ct)
    {
        // Reads go through HybridCache: L1 in-process first, then Garnet, and
        // only then the database. State is passed explicitly so the factory
        // stays a static lambda and allocates nothing per call.
        return cache.GetOrCreateAsync(
            CacheKeys.Order(query.OrderId),
            (orders, query.OrderId),
            static (state, token) => new ValueTask<OrderDto?>(state.orders.GetAsync(state.OrderId, token)),
            expiration: CacheDuration,
            localExpiration: TimeSpan.FromSeconds(30),
            ct: ct);
    }
}
