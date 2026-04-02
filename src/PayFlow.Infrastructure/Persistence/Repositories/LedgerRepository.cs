using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Infrastructure.Persistence.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly PayFlowDbContext _context;

    public LedgerRepository(PayFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddEntriesAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        await _context.LedgerEntries.AddRangeAsync(entries, cancellationToken);
    }

    public async Task<IEnumerable<LedgerEntry>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .Where(le => le.TransactionId == transactionId)
            .OrderBy(le => le.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
