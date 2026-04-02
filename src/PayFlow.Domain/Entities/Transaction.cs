using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities;

public class Transaction
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public Guid SourceWalletId { get; private set; }
    public Guid? DestinationWalletId { get; private set; }
    public long Amount { get; private set; }              // Minor units
    public string Currency { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? FailureReason { get; private set; }
    public Guid? OriginalTransactionId { get; private set; } // For refunds
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Navigation
    public Wallet? SourceWallet { get; private set; }
    public Wallet? DestinationWallet { get; private set; }
    public Transaction? OriginalTransaction { get; private set; }
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = new List<LedgerEntry>();

    private Transaction() { } // EF Core

    public static Transaction Create(
        string idempotencyKey,
        TransactionType type,
        Guid sourceWalletId,
        Guid? destinationWalletId,
        long amount,
        string currency,
        string? description = null,
        Guid? originalTransactionId = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a valid ISO 4217 code.", nameof(currency));

        return new Transaction
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            Type = type,
            Status = TransactionStatus.Pending,
            SourceWalletId = sourceWalletId,
            DestinationWalletId = destinationWalletId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Description = description,
            OriginalTransactionId = originalTransactionId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessing()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot move to Processing from {Status}.");

        Status = TransactionStatus.Processing;
    }

    public void MarkCompleted()
    {
        if (Status != TransactionStatus.Pending && Status != TransactionStatus.Processing)
            throw new InvalidOperationException($"Cannot complete transaction in {Status} status.");

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        if (Status == TransactionStatus.Completed || Status == TransactionStatus.Reversed)
            throw new InvalidOperationException($"Cannot fail transaction in {Status} status.");

        Status = TransactionStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkReversed()
    {
        if (Status != TransactionStatus.Completed)
            throw new InvalidOperationException("Only completed transactions can be reversed.");

        Status = TransactionStatus.Reversed;
    }
}
