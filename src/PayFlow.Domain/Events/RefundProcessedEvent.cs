namespace PayFlow.Domain.Events;

public class RefundProcessedEvent : DomainEvent
{
    public Guid RefundTransactionId { get; }
    public Guid OriginalTransactionId { get; }
    public long Amount { get; }
    public string Currency { get; }

    public RefundProcessedEvent(Guid refundTransactionId, Guid originalTransactionId, long amount, string currency)
    {
        EventType = "refund.processed";
        RefundTransactionId = refundTransactionId;
        OriginalTransactionId = originalTransactionId;
        Amount = amount;
        Currency = currency;
    }
}
