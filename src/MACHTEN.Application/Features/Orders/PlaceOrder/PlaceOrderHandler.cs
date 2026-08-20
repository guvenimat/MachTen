using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Features.Orders.PlaceOrder;

/// <summary>
/// Wolverine discovers this by convention (Handle/HandleAsync on a *Handler
/// type) and generates the dispatch code at build time.
/// </summary>
public static class PlaceOrderHandler
{
    public static async Task<PlaceOrderResponse> HandleAsync(
        PlaceOrderCommand command,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var order = Order.Place(command.CustomerReference, new Money(command.Amount, command.Currency));

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return new PlaceOrderResponse(order.Id, order.Total.Formatted);
    }
}
