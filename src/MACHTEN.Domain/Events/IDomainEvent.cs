namespace MACHTEN.Domain.Events;

/// <summary>
/// Marker for things that have already happened. Domain events are recorded on
/// the aggregate and published only after the transaction commits, so nothing
/// downstream reacts to a change that later rolls back.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
