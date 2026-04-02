namespace PayFlow.Domain.Exceptions;

public class WalletNotFoundException : Exception
{
    public Guid WalletId { get; }

    public WalletNotFoundException(Guid walletId)
        : base($"Wallet {walletId} was not found.")
    {
        WalletId = walletId;
    }
}
