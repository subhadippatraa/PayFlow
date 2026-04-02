using System.Data;

namespace PayFlow.Domain.Interfaces;

public interface IUnitOfWork
{
    IWalletRepository Wallets { get; }
    ITransactionRepository Transactions { get; }
    ILedgerRepository Ledgers { get; }

    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
