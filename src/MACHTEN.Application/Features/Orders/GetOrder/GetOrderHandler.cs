using MACHTEN.Application.Contracts;
using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Application.Mapping;
using Microsoft.EntityFrameworkCore;

namespace MACHTEN.Application.Features.Orders.GetOrder;

public static class GetOrderHandler
{
    /// <summary>Long enough to absorb a burst, short enough that staleness stays boring.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public static ValueTask<OrderDto?> HandleAsync(
        GetOrderQuery query,
        IApplicationDbContext db,
        ICacheStore cache,
        CancellationToken ct)
    {
        // Reads go through HybridCache: L1 in-process first, then Garnet, and
        // only then the database. State is passed explicitly so the factory
        // stays a static lambda and allocates nothing per call.
        return cache.GetOrCreateAsync(
            CacheKeys.Order(query.OrderId),
            (db, query.OrderId),
            static async (state, token) =>
            {
                var order = await state.db.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == state.OrderId, token);

                return order is null ? null : OrderMapper.ToDto(order);
            },
            expiration: CacheDuration,
            localExpiration: TimeSpan.FromSeconds(30),
            ct: ct);
    }
}
