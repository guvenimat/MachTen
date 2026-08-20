namespace MACHTEN.Application.Features.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId);

public sealed record OrderDto(
    Guid Id,
    string CustomerReference,
    decimal Amount,
    string Currency,
    string Formatted,
    string Status,
    DateTimeOffset CreatedAtUtc);
