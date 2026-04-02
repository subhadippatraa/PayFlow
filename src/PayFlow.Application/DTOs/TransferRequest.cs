namespace PayFlow.Application.DTOs;

public class TransferRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid SourceWalletId { get; set; }
    public Guid DestinationWalletId { get; set; }
    public long Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}
