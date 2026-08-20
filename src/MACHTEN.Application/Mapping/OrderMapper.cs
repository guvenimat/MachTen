using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace MACHTEN.Application.Mapping;

/// <summary>
/// Flattens the Money value object onto the DTO. Mapperly generates this at
/// compile time, so the mapping shows up in the debugger and breaks the build
/// — rather than a request — when a property stops lining up.
/// </summary>
[Mapper]
public static partial class OrderMapper
{
    [MapperIgnoreSource(nameof(Order.DomainEvents))]
    [MapProperty([nameof(Order.Total), nameof(Order.Total.Amount)], [nameof(OrderDto.Amount)])]
    [MapProperty([nameof(Order.Total), nameof(Order.Total.Currency)], [nameof(OrderDto.Currency)])]
    [MapProperty([nameof(Order.Total), nameof(Order.Total.Formatted)], [nameof(OrderDto.Formatted)])]
    public static partial OrderDto ToDto(Order order);

    private static string MapStatus(OrderStatus status) => status.ToString();
}
