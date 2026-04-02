using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

/// <summary>
/// Immutable ledger entry for double-entry bookkeeping.
/// Every transaction produces exactly one Debit and one Credit entry.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid WalletId { get; private set; }
    public LedgerEntryType EntryType { get; private set; }
    public long Amount { get; private set; }               // Always positive
    public string Currency { get; private set; } = string.Empty;
    public long RunningBalance { get; private set; }       // Balance after this entry
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Transaction? Transaction { get; private set; }
    public Wallet? Wallet { get; private set; }

    private LedgerEntry() { } // EF Core

    public static LedgerEntry Create(
        Guid transactionId,
        Guid walletId,
        LedgerEntryType entryType,
        long amount,
        string currency,
        long runningBalance)
    {
        if (amount <= 0)
            throw new ArgumentException("Ledger entry amount must be positive.", nameof(amount));

        return new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            WalletId = walletId,
            EntryType = entryType,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            RunningBalance = runningBalance,
            CreatedAt = DateTime.UtcNow
        };
    }
}
