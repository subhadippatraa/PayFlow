namespace PayFlow.Application.DTOs;

public class RefundRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid OriginalTransactionId { get; set; }

    /// <summary>
    /// Amount to refund in minor units. If null, a full refund is issued.
    /// </summary>
    public long? Amount { get; set; }

    public string? Reason { get; set; }
}
