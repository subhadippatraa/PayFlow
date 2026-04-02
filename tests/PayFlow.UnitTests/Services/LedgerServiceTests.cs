using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using FluentAssertions;

namespace PayFlow.UnitTests.Services;

public class LedgerServiceTests
{
    private readonly LedgerService _sut;

    public LedgerServiceTests()
    {
        _sut = new LedgerService();
    }

    [Fact]
    public void RecordTransfer_CreatesDebitAndCreditEntries()
    {
        // Arrange
        var sourceWallet = Wallet.Create(Guid.NewGuid(), "USD");
        sourceWallet.Credit(100000);

        var destWallet = Wallet.Create(Guid.NewGuid(), "USD");

        var transaction = Transaction.Create(
            idempotencyKey: "ledger_test",
            type: TransactionType.Transfer,
            sourceWalletId: sourceWallet.Id,
            destinationWalletId: destWallet.Id,
            amount: 50000,
            currency: "USD"
        );

        // Simulate the debit/credit that PaymentService would do
        sourceWallet.Debit(50000);
        destWallet.Credit(50000);

        // Act
        _sut.RecordTransfer(transaction, sourceWallet, destWallet);

        // Assert
        transaction.LedgerEntries.Should().HaveCount(2);

        var debitEntry = transaction.LedgerEntries.Single(e => e.EntryType == LedgerEntryType.Debit);
        debitEntry.WalletId.Should().Be(sourceWallet.Id);
        debitEntry.Amount.Should().Be(50000);
        debitEntry.RunningBalance.Should().Be(50000); // 100000 - 50000

        var creditEntry = transaction.LedgerEntries.Single(e => e.EntryType == LedgerEntryType.Credit);
        creditEntry.WalletId.Should().Be(destWallet.Id);
        creditEntry.Amount.Should().Be(50000);
        creditEntry.RunningBalance.Should().Be(50000); // 0 + 50000
    }

    [Fact]
    public void RecordTransfer_DoubleEntryInvariant_CreditsEqualDebits()
    {
        var sourceWallet = Wallet.Create(Guid.NewGuid(), "INR");
        sourceWallet.Credit(1000000);
        var destWallet = Wallet.Create(Guid.NewGuid(), "INR");

        var transaction = Transaction.Create(
            "de_test", TransactionType.Transfer, sourceWallet.Id, destWallet.Id, 250000, "INR");

        sourceWallet.Debit(250000);
        destWallet.Credit(250000);

        _sut.RecordTransfer(transaction, sourceWallet, destWallet);

        var totalDebits = transaction.LedgerEntries
            .Where(e => e.EntryType == LedgerEntryType.Debit).Sum(e => e.Amount);
        var totalCredits = transaction.LedgerEntries
            .Where(e => e.EntryType == LedgerEntryType.Credit).Sum(e => e.Amount);

        totalCredits.Should().Be(totalDebits, "double-entry invariant: credits must equal debits");
    }

    [Fact]
    public void RecordTopUp_CreatesCreditEntry()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(100000);

        var transaction = Transaction.Create(
            "topup_ledger", TransactionType.TopUp, wallet.Id, wallet.Id, 100000, "USD");

        _sut.RecordTopUp(transaction, wallet);

        transaction.LedgerEntries.Should().HaveCount(1);

        var entry = transaction.LedgerEntries.Single();
        entry.EntryType.Should().Be(LedgerEntryType.Credit);
        entry.Amount.Should().Be(100000);
        entry.WalletId.Should().Be(wallet.Id);
    }
}
