using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly PayFlowDbContext _context;

    public TransactionRepository(PayFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.LedgerEntries)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPaginatedAsync(
        Guid? walletId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions.AsQueryable();

        if (walletId.HasValue)
        {
            query = query.Where(t => t.SourceWalletId == walletId.Value
                                  || t.DestinationWalletId == walletId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.LedgerEntries)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public void Update(Transaction transaction)
    {
        _context.Transactions.Update(transaction);
    }
}
