namespace PayFlow.Domain.Events;

public class PaymentCompletedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string TransactionType { get; }
    public long Amount { get; }
    public string Currency { get; }
    public Guid SourceWalletId { get; }
    public Guid? DestinationWalletId { get; }

    public PaymentCompletedEvent(
        Guid transactionId, string transactionType, long amount, string currency,
        Guid sourceWalletId, Guid? destinationWalletId)
    {
        EventType = "payment.completed";
        TransactionId = transactionId;
        TransactionType = transactionType;
        Amount = amount;
        Currency = currency;
        SourceWalletId = sourceWalletId;
        DestinationWalletId = destinationWalletId;
    }
}
