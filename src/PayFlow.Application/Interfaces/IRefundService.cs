using PayFlow.Application.DTOs;

namespace PayFlow.Application.Interfaces;

public interface IRefundService
{
    Task<TransactionResult> ProcessRefundAsync(RefundRequest request, CancellationToken cancellationToken = default);
}
