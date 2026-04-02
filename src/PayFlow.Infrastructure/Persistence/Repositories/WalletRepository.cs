using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly PayFlowDbContext _context;

    public WalletRepository(PayFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Wallet?> GetByUserIdAndCurrencyAsync(Guid userId, string currency, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Currency == currency.ToUpperInvariant(), cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }

    public void Update(Wallet wallet)
    {
        _context.Wallets.Update(wallet);
    }
}
