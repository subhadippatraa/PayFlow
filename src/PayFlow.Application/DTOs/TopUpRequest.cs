namespace PayFlow.Application.DTOs;

public class TopUpRequest
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid WalletId { get; set; }
    public long Amount { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}
