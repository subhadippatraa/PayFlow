using PayFlow.Application.DTOs;
using PayFlow.Domain.Entities;

namespace PayFlow.Application.Interfaces;

public interface IWalletService
{
    Task<Wallet> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default);
    Task<Wallet?> GetWalletAsync(Guid walletId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Wallet>> GetWalletsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
