using PayFlow.Application.DTOs;

namespace PayFlow.Application.Interfaces;

public interface IPaymentService
{
    Task<TransactionResult> ProcessTransferAsync(TransferRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResult> ProcessTopUpAsync(TopUpRequest request, CancellationToken cancellationToken = default);
}
