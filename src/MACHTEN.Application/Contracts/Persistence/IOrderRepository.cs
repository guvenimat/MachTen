using MACHTEN.Application.Features.Orders.GetOrder;
using MACHTEN.Domain.Entities;

namespace MACHTEN.Application.Contracts.Persistence;

/// <summary>
/// Order persistence, expressed without any ORM in the signature.
///
/// Reads return the DTO rather than the aggregate on purpose. Order guards its
/// invariants with private setters and a private constructor, so rehydrating it
/// from columns would mean either loosening the domain model or reflecting over
/// it — and the read path only ever needs the flat shape anyway.
/// </summary>
public interface IOrderRepository
{
    Task InsertAsync(Order order, CancellationToken ct = default);

    Task<OrderDto?> GetAsync(Guid id, CancellationToken ct = default);
}
