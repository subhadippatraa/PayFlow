using PayFlow.Domain.Entities;

namespace PayFlow.Domain.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Wallet?> GetByUserIdAndCurrencyAsync(Guid userId, string currency, CancellationToken cancellationToken = default);
    Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    void Update(Wallet wallet);
}
