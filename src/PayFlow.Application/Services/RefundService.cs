using System.Data;
using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Events;
using PayFlow.Domain.Exceptions;
using PayFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace PayFlow.Application.Services;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerService _ledgerService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        IUnitOfWork unitOfWork,
        ILedgerService ledgerService,
        IEventPublisher eventPublisher,
        ILogger<RefundService> logger)
    {
        _unitOfWork = unitOfWork;
        _ledgerService = ledgerService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<TransactionResult> ProcessRefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            // 1. Load original transaction
            var originalTransaction = await _unitOfWork.Transactions.GetByIdAsync(
                request.OriginalTransactionId, cancellationToken);

            if (originalTransaction == null)
                return TransactionResult.Failure("Original transaction not found.");

            if (originalTransaction.Status != TransactionStatus.Completed)
                return TransactionResult.Failure(
                    $"Cannot refund transaction in {originalTransaction.Status} status. Only completed transactions can be refunded.");

            // 2. Determine refund amount
            var refundAmount = request.Amount ?? originalTransaction.Amount;

            if (refundAmount <= 0 || refundAmount > originalTransaction.Amount)
                return TransactionResult.Failure(
                    $"Invalid refund amount {refundAmount}. Must be between 1 and {originalTransaction.Amount}.");

            // 3. Load wallets — refund reverses the flow
            //    Original: source → destination  
            //    Refund:   destination → source (debit destination, credit source)
            var sourceWallet = await _unitOfWork.Wallets.GetByIdAsync(
                originalTransaction.SourceWalletId, cancellationToken);

            Wallet? destinationWallet = null;
            if (originalTransaction.DestinationWalletId.HasValue)
            {
                destinationWallet = await _unitOfWork.Wallets.GetByIdAsync(
                    originalTransaction.DestinationWalletId.Value, cancellationToken);
            }

            if (sourceWallet == null)
                return TransactionResult.Failure("Source wallet from original transaction not found.");

            // 4. Create refund transaction
            var refundTransaction = Transaction.Create(
                idempotencyKey: request.IdempotencyKey,
                type: TransactionType.Refund,
                sourceWalletId: destinationWallet?.Id ?? sourceWallet.Id,
                destinationWalletId: sourceWallet.Id,
                amount: refundAmount,
                currency: originalTransaction.Currency,
                description: request.Reason ?? $"Refund for transaction {originalTransaction.Id}",
                originalTransactionId: originalTransaction.Id
            );

            // 5. Reverse wallet balances
            if (destinationWallet != null)
            {
                // Transfer refund: debit destination, credit source
                destinationWallet.Debit(refundAmount);
                sourceWallet.Credit(refundAmount);

                _ledgerService.RecordTransfer(refundTransaction, destinationWallet, sourceWallet);
            }
            else
            {
                // Top-up refund: just debit the source wallet
                sourceWallet.Debit(refundAmount);

                var debitEntry = LedgerEntry.Create(
                    transactionId: refundTransaction.Id,
                    walletId: sourceWallet.Id,
                    entryType: LedgerEntryType.Debit,
                    amount: refundAmount,
                    currency: originalTransaction.Currency,
                    runningBalance: sourceWallet.Balance
                );
                refundTransaction.LedgerEntries.Add(debitEntry);
            }

            // 6. Mark transition states
            refundTransaction.MarkCompleted();

            // Mark original as reversed only if it's a full refund
            if (refundAmount == originalTransaction.Amount)
                originalTransaction.MarkReversed();

            // 7. Persist
            await _unitOfWork.Transactions.AddAsync(refundTransaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 8. Publish event
            await _eventPublisher.PublishAsync(
                new RefundProcessedEvent(
                    refundTransaction.Id, originalTransaction.Id,
                    refundAmount, originalTransaction.Currency),
                cancellationToken);

            _logger.LogInformation(
                "Refund {RefundId} processed for original transaction {OriginalId}, amount {Amount}",
                refundTransaction.Id, originalTransaction.Id, refundAmount);

            return TransactionResult.Success(refundTransaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new ConcurrencyConflictException("Wallet was modified by another transaction. Retry.");
        }
        catch (InsufficientFundsException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return TransactionResult.Failure(ex.Message);
        }
        catch (Exception ex) when (ex is not ConcurrencyConflictException)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process refund for transaction {TransactionId}",
                request.OriginalTransactionId);
            throw;
        }
    }
}
