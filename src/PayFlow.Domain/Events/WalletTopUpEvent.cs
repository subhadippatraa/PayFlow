namespace PayFlow.Domain.Events;

public class WalletTopUpEvent : DomainEvent
{
    public Guid WalletId { get; }
    public long Amount { get; }
    public string Currency { get; }
    public Guid TransactionId { get; }

    public WalletTopUpEvent(Guid walletId, long amount, string currency, Guid transactionId)
    {
        EventType = "wallet.topup";
        WalletId = walletId;
        Amount = amount;
        Currency = currency;
        TransactionId = transactionId;
    }
}
