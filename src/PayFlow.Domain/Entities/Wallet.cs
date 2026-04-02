using PayFlow.Domain.Exceptions;

namespace PayFlow.Domain.Entities;

public class Wallet
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; } = string.Empty; // ISO 4217: "USD", "INR"
    public long Balance { get; private set; }           // In minor units (cents/paise)
    public long HeldBalance { get; private set; }       // Funds on hold (pending txns)
    public bool IsActive { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>(); // Optimistic concurrency token
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation
    public User? User { get; private set; }
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = new List<LedgerEntry>();

    /// <summary>
    /// Balance available for withdrawal/transfer (excludes held funds).
    /// </summary>
    public long AvailableBalance => Balance - HeldBalance;

    private Wallet() { } // EF Core

    public static Wallet Create(Guid userId, string currency)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a valid ISO 4217 code.", nameof(currency));

        return new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Currency = currency.ToUpperInvariant(),
            Balance = 0,
            HeldBalance = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Debit the wallet. Throws InsufficientFundsException if available balance is too low.
    /// </summary>
    public void Debit(long amount)
    {
        if (amount <= 0) throw new ArgumentException("Debit amount must be positive.", nameof(amount));
        if (!IsActive) throw new WalletNotFoundException(Id);
        if (AvailableBalance < amount)
            throw new InsufficientFundsException(Id, amount, AvailableBalance);

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Credit the wallet.
    /// </summary>
    public void Credit(long amount)
    {
        if (amount <= 0) throw new ArgumentException("Credit amount must be positive.", nameof(amount));
        if (!IsActive) throw new WalletNotFoundException(Id);

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Place a hold on funds (e.g., pending authorization).
    /// </summary>
    public void HoldFunds(long amount)
    {
        if (amount <= 0) throw new ArgumentException("Hold amount must be positive.", nameof(amount));
        if (AvailableBalance < amount)
            throw new InsufficientFundsException(Id, amount, AvailableBalance);

        HeldBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Release previously held funds.
    /// </summary>
    public void ReleaseFunds(long amount)
    {
        if (amount <= 0) throw new ArgumentException("Release amount must be positive.", nameof(amount));
        if (HeldBalance < amount)
            throw new InvalidOperationException($"Cannot release {amount}: only {HeldBalance} is held.");

        HeldBalance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Balance > 0)
            throw new InvalidOperationException("Cannot deactivate wallet with positive balance.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
