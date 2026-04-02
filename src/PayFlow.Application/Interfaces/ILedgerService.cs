using PayFlow.Domain.Entities;

namespace PayFlow.Application.Interfaces;

public interface ILedgerService
{
    void RecordTransfer(Transaction transaction, Wallet sourceWallet, Wallet destinationWallet);
    void RecordTopUp(Transaction transaction, Wallet wallet);
}
