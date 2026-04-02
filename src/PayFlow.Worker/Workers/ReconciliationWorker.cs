using PayFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PayFlow.Worker.Workers;

/// <summary>
/// Background worker that periodically runs ledger reconciliation
/// to verify the double-entry bookkeeping invariant:
/// SUM(Credits) == SUM(Debits) for every transaction.
/// Also verifies wallet balances match the running sum of ledger entries.
/// </summary>
public class ReconciliationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliationWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public ReconciliationWorker(IServiceScopeFactory scopeFactory, ILogger<ReconciliationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Reconciliation worker started, running every {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunReconciliationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation check failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Reconciliation worker stopped");
    }

    private async Task RunReconciliationAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();

        _logger.LogInformation("Starting ledger reconciliation check...");

        // Check 1: Verify debit/credit balance per transaction
        var imbalancedTransactions = await context.LedgerEntries
            .GroupBy(le => le.TransactionId)
            .Select(g => new
            {
                TransactionId = g.Key,
                TotalCredits = g.Where(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit).Sum(e => e.Amount),
                TotalDebits = g.Where(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit).Sum(e => e.Amount)
            })
            .Where(x => x.TotalCredits != x.TotalDebits)
            .ToListAsync(cancellationToken);

        if (imbalancedTransactions.Any())
        {
            foreach (var imbalance in imbalancedTransactions)
            {
                _logger.LogCritical(
                    "LEDGER IMBALANCE DETECTED: Transaction {TransactionId} has Credits={Credits} Debits={Debits}",
                    imbalance.TransactionId, imbalance.TotalCredits, imbalance.TotalDebits);
            }
        }
        else
        {
            _logger.LogInformation("Ledger reconciliation passed: all transactions balanced");
        }

        // Check 2: Verify wallet balances match ledger net amounts
        var walletChecks = await context.LedgerEntries
            .GroupBy(le => le.WalletId)
            .Select(g => new
            {
                WalletId = g.Key,
                LedgerBalance = g.Where(e => e.EntryType == Domain.Enums.LedgerEntryType.Credit).Sum(e => e.Amount)
                              - g.Where(e => e.EntryType == Domain.Enums.LedgerEntryType.Debit).Sum(e => e.Amount)
            })
            .ToListAsync(cancellationToken);

        foreach (var check in walletChecks)
        {
            var wallet = await context.Wallets.FindAsync(new object[] { check.WalletId }, cancellationToken);
            if (wallet != null && wallet.Balance != check.LedgerBalance)
            {
                _logger.LogCritical(
                    "WALLET BALANCE MISMATCH: Wallet {WalletId} has Balance={WalletBalance} but Ledger shows {LedgerBalance}",
                    check.WalletId, wallet.Balance, check.LedgerBalance);
            }
        }

        _logger.LogInformation("Reconciliation check completed, checked {Count} wallets", walletChecks.Count);
    }
}
