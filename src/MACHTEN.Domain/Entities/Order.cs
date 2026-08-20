using MACHTEN.Domain.Events;
using MACHTEN.Domain.Exceptions;
using MACHTEN.Domain.ValueObjects;

namespace MACHTEN.Domain.Entities;

public sealed class Order
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public string CustomerReference { get; private set; } = string.Empty;
    public Money Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Events raised since the aggregate was loaded, awaiting publication.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    private Order() { }

    public static Order Place(string customerReference, Money total)
    {
        if (string.IsNullOrWhiteSpace(customerReference))
            throw new InvalidOrderException("Customer reference is required.");

        if (total.Amount <= 0)
            throw new InvalidOrderException("An order total must be greater than zero.");

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerReference = customerReference.Trim(),
            Total = total,
            Status = OrderStatus.Placed,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        order._domainEvents.Add(new OrderPlaced(
            order.Id,
            order.CustomerReference,
            total.Amount,
            total.Currency,
            order.CreatedAtUtc));

        return order;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
