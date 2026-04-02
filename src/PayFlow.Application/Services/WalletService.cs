using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace PayFlow.Application.Services;

public class WalletService : IWalletService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WalletService> _logger;

    public WalletService(IUnitOfWork unitOfWork, ILogger<WalletService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Wallet> CreateWalletAsync(CreateWalletRequest request, CancellationToken cancellationToken = default)
    {
        // Check for existing wallet with same user + currency
        var existing = await _unitOfWork.Wallets.GetByUserIdAndCurrencyAsync(
            request.UserId, request.Currency, cancellationToken);

        if (existing != null)
            throw new InvalidOperationException(
                $"User {request.UserId} already has a {request.Currency} wallet.");

        var wallet = Wallet.Create(request.UserId, request.Currency);

        await _unitOfWork.Wallets.AddAsync(wallet, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletId} created for user {UserId} in {Currency}",
            wallet.Id, wallet.UserId, wallet.Currency);

        return wallet;
    }

    public async Task<Wallet?> GetWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Wallets.GetByIdAsync(walletId, cancellationToken);
    }

    public async Task<IEnumerable<Wallet>> GetWalletsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Wallets.GetByUserIdAsync(userId, cancellationToken);
    }
}
