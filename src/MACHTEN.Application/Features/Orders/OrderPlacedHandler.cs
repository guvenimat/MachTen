using MACHTEN.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MACHTEN.Application.Features.Orders;

/// <summary>
/// Consumes the event the outbox delivers after the order transaction commits.
/// Replace the body with the real reaction — notify, project, forward to a
/// broker; the delivery guarantee is already handled.
/// </summary>
public static class OrderPlacedHandler
{
    public static void Handle(OrderPlaced message, ILogger<OrderPlaced> logger)
    {
        logger.LogInformation(
            "Order {OrderId} placed for {CustomerReference}: {Amount} {Currency}",
            message.OrderId,
            message.CustomerReference,
            message.Amount,
            message.Currency);
    }
}
