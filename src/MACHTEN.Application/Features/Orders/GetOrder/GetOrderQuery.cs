using MemoryPack;

namespace MACHTEN.Application.Features.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId);

/// <summary>
/// Cached, so it is serialized on every L2 write and read. [MemoryPackable]
/// generates a binary formatter at compile time — no reflection, and far
/// smaller and faster than the JSON HybridCache would otherwise fall back to.
/// The type must be partial for the generator to extend it.
/// </summary>
[MemoryPackable]
public sealed partial record OrderDto(
    Guid Id,
    string CustomerReference,
    decimal Amount,
    string Currency,
    string Formatted,
    string Status,
    DateTimeOffset CreatedAtUtc);
