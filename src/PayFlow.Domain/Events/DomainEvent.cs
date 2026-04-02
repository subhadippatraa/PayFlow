namespace PayFlow.Domain.Events;

/// <summary>
/// Base class for all domain events.
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType { get; protected set; } = string.Empty;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}
