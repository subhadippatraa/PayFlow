namespace PayFlow.Api.DTOs.Requests;

public class CreateWalletApiRequest
{
    public Guid UserId { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class TopUpApiRequest
{
    public long Amount { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}

public class TransferApiRequest
{
    public Guid SourceWalletId { get; set; }
    public Guid DestinationWalletId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RefundApiRequest
{
    public long? Amount { get; set; }
    public string? Reason { get; set; }
}
