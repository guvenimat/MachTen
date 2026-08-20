namespace MACHTEN.Domain.Events;

public sealed record OrderPlaced(
    Guid OrderId,
    string CustomerReference,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
