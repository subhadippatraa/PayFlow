using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Events;
using PayFlow.Domain.Exceptions;
using PayFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILedgerService _ledgerService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ILedgerService ledgerService,
        IEventPublisher eventPublisher,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _ledgerService = ledgerService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<TransactionResult> ProcessTransferAsync(TransferRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        Transaction? transaction = null;

        try
        {
            var sourceWallet = await _unitOfWork.Wallets.GetByIdAsync(request.SourceWalletId, cancellationToken);
            var destWallet = await _unitOfWork.Wallets.GetByIdAsync(request.DestinationWalletId, cancellationToken);

            if (sourceWallet == null || destWallet == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return TransactionResult.Failure("One or both wallets were not found.");
            }

            if (sourceWallet.Currency != destWallet.Currency)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return TransactionResult.Failure("Currency mismatch between source and destination wallets.");
            }

            transaction = Transaction.Create(
                idempotencyKey: request.IdempotencyKey,
                type: TransactionType.Transfer,
                sourceWalletId: sourceWallet.Id,
                destinationWalletId: destWallet.Id,
                amount: request.Amount,
                currency: sourceWallet.Currency,
                description: request.Description
            );

            sourceWallet.Debit(request.Amount);
            destWallet.Credit(request.Amount);

            _ledgerService.RecordTransfer(transaction, sourceWallet, destWallet);
            transaction.MarkCompleted();

            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Publish event outside the database transaction, outbox pattern typically catches this
            await _eventPublisher.PublishAsync(
                new PaymentCompletedEvent(
                    transaction.Id, transaction.Type.ToString(), transaction.Amount,
                    transaction.Currency, transaction.SourceWalletId, transaction.DestinationWalletId),
                cancellationToken);

            return TransactionResult.Success(transaction);
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
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process transfer");
            throw;
        }
    }

    public async Task<TransactionResult> ProcessTopUpAsync(TopUpRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        Transaction? transaction = null;

        try
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(request.WalletId, cancellationToken);
            if (wallet == null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return TransactionResult.Failure("Wallet not found.");
            }

            transaction = Transaction.Create(
                idempotencyKey: request.IdempotencyKey,
                type: TransactionType.TopUp,
                sourceWalletId: wallet.Id,
                destinationWalletId: wallet.Id,
                amount: request.Amount,
                currency: wallet.Currency,
                description: $"Top-up via reference {request.ReferenceId}"
            );

            wallet.Credit(request.Amount);

            _ledgerService.RecordTopUp(transaction, wallet);
            transaction.MarkCompleted();

            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                new PaymentCompletedEvent(
                    transaction.Id, transaction.Type.ToString(), transaction.Amount,
                    transaction.Currency, transaction.SourceWalletId, transaction.DestinationWalletId),
                cancellationToken);

            return TransactionResult.Success(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw new ConcurrencyConflictException("Wallet was modified by another transaction. Retry.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process top-up");
            throw;
        }
    }
}
