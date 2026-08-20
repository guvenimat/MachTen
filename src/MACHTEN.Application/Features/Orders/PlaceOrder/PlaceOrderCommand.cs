namespace MACHTEN.Application.Features.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(string CustomerReference, decimal Amount, string Currency);

public sealed record PlaceOrderResponse(Guid OrderId, string Total);
