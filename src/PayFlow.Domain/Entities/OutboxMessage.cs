namespace PayFlow.Domain.Entities;

/// <summary>
/// Transactional outbox message for guaranteed event delivery.
/// Events are persisted atomically with the transaction, then published by a background job.
/// </summary>
public class OutboxMessage
{
    public long Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(string eventType, string payload, string? correlationId = null)
    {
        return new OutboxMessage
        {
            EventType = eventType,
            Payload = payload,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkPublished()
    {
        PublishedAt = DateTime.UtcNow;
    }
}
