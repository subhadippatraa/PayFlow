namespace PayFlow.Application.DTOs;

public class CreateWalletRequest
{
    public Guid UserId { get; set; }
    public string Currency { get; set; } = string.Empty;
}
