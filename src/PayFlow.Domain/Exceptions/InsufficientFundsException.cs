namespace PayFlow.Domain.Exceptions;

public class InsufficientFundsException : Exception
{
    public Guid WalletId { get; }
    public long RequestedAmount { get; }
    public long AvailableBalance { get; }

    public InsufficientFundsException(Guid walletId, long requestedAmount, long availableBalance)
        : base($"Wallet {walletId} has available balance {availableBalance} but {requestedAmount} was requested.")
    {
        WalletId = walletId;
        RequestedAmount = requestedAmount;
        AvailableBalance = availableBalance;
    }
}
