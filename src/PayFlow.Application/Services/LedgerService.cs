using PayFlow.Application.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;

namespace PayFlow.Application.Services;

public class LedgerService : ILedgerService
{
    public void RecordTransfer(Transaction transaction, Wallet sourceWallet, Wallet destinationWallet)
    {
        var debitEntry = LedgerEntry.Create(
            transactionId: transaction.Id,
            walletId: sourceWallet.Id,
            entryType: LedgerEntryType.Debit,
            amount: transaction.Amount,
            currency: transaction.Currency,
            runningBalance: sourceWallet.Balance
        );

        var creditEntry = LedgerEntry.Create(
            transactionId: transaction.Id,
            walletId: destinationWallet.Id,
            entryType: LedgerEntryType.Credit,
            amount: transaction.Amount,
            currency: transaction.Currency,
            runningBalance: destinationWallet.Balance
        );

        transaction.LedgerEntries.Add(debitEntry);
        transaction.LedgerEntries.Add(creditEntry);
    }
    
    public void RecordTopUp(Transaction transaction, Wallet wallet)
    {
        var creditEntry = LedgerEntry.Create(
            transactionId: transaction.Id,
            walletId: wallet.Id,
            entryType: LedgerEntryType.Credit,
            amount: transaction.Amount,
            currency: transaction.Currency,
            runningBalance: wallet.Balance
        );

        transaction.LedgerEntries.Add(creditEntry);
        // Note: For top-ups, the debit side would be a platform bank account, but keeping it simple for the wallet focus.
    }
}
