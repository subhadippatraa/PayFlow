using PayFlow.Domain.Entities;

namespace PayFlow.Domain.Interfaces;

public interface ILedgerRepository
{
    Task AddEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default);
    Task<IEnumerable<LedgerEntry>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
