namespace PayFlow.Domain.Events;

public class PaymentFailedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string Reason { get; }

    public PaymentFailedEvent(Guid transactionId, string reason)
    {
        EventType = "payment.failed";
        TransactionId = transactionId;
        Reason = reason;
    }
}
