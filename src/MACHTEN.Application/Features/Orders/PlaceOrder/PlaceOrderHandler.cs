using MACHTEN.Application.Contracts.Persistence;
using MACHTEN.Domain.Entities;
using MACHTEN.Domain.Events;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Application.Features.Orders.PlaceOrder;

/// <summary>
/// Wolverine discovers this by convention (Handle/HandleAsync on a *Handler
/// type) and generates the dispatch code at build time.
/// </summary>
public static class PlaceOrderHandler
{
    /// <summary>
    /// Returns the response plus the event to publish. Wolverine treats the
    /// first tuple member as the reply and the rest as cascading messages,
    /// routing them through the outbox — so the event is written in the same
    /// transaction as the order and delivered only after it commits.
    ///
    /// Returning it matters: the aggregate records the event, but recording is
    /// not publishing. Before this, Order.Place raised OrderPlaced and the
    /// handler dropped it on the floor.
    /// </summary>
    public static async Task<(PlaceOrderResponse Response, OrderPlaced Published)> HandleAsync(
        PlaceOrderCommand command,
        IApplicationDbContext db,
        CancellationToken ct)
    {
        var order = Order.Place(command.CustomerReference, new Money(command.Amount, command.Currency));

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var placed = order.DomainEvents.OfType<OrderPlaced>().Single();
        order.ClearDomainEvents();

        return (new PlaceOrderResponse(order.Id, order.Total.Formatted), placed);
    }
}
