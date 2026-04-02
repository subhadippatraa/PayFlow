using PayFlow.Domain.Entities;

namespace PayFlow.Api.DTOs.Responses;

public class WalletResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public long Balance { get; set; }
    public long HeldBalance { get; set; }
    public long AvailableBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static WalletResponse FromEntity(Wallet wallet)
    {
        return new WalletResponse
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Currency = wallet.Currency,
            Balance = wallet.Balance,
            HeldBalance = wallet.HeldBalance,
            AvailableBalance = wallet.AvailableBalance,
            IsActive = wallet.IsActive,
            CreatedAt = wallet.CreatedAt
        };
    }
}

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid SourceWalletId { get; set; }
    public Guid? DestinationWalletId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FailureReason { get; set; }
    public Guid? OriginalTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public static TransactionResponse FromEntity(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            SourceWalletId = transaction.SourceWalletId,
            DestinationWalletId = transaction.DestinationWalletId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Description = transaction.Description,
            FailureReason = transaction.FailureReason,
            OriginalTransactionId = transaction.OriginalTransactionId,
            CreatedAt = transaction.CreatedAt,
            CompletedAt = transaction.CompletedAt
        };
    }
}

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
