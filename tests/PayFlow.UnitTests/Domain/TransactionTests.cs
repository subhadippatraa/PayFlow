using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using FluentAssertions;

namespace PayFlow.UnitTests.Domain;

public class TransactionTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsTransaction()
    {
        var sourceId = Guid.NewGuid();
        var destId = Guid.NewGuid();

        var transaction = Transaction.Create(
            idempotencyKey: "test_key_123",
            type: TransactionType.Transfer,
            sourceWalletId: sourceId,
            destinationWalletId: destId,
            amount: 50000,
            currency: "INR",
            description: "Test transfer"
        );

        transaction.Id.Should().NotBeEmpty();
        transaction.IdempotencyKey.Should().Be("test_key_123");
        transaction.Type.Should().Be(TransactionType.Transfer);
        transaction.Status.Should().Be(TransactionStatus.Pending);
        transaction.SourceWalletId.Should().Be(sourceId);
        transaction.DestinationWalletId.Should().Be(destId);
        transaction.Amount.Should().Be(50000);
        transaction.Currency.Should().Be("INR");
    }

    [Fact]
    public void Create_EmptyIdempotencyKey_ThrowsArgumentException()
    {
        var act = () => Transaction.Create(
            "", TransactionType.Transfer, Guid.NewGuid(), Guid.NewGuid(), 100, "USD");

        act.Should().Throw<ArgumentException>().WithMessage("*Idempotency key*");
    }

    [Fact]
    public void Create_ZeroAmount_ThrowsArgumentException()
    {
        var act = () => Transaction.Create(
            "key", TransactionType.Transfer, Guid.NewGuid(), Guid.NewGuid(), 0, "USD");

        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_InvalidCurrency_ThrowsArgumentException()
    {
        var act = () => Transaction.Create(
            "key", TransactionType.Transfer, Guid.NewGuid(), Guid.NewGuid(), 100, "US");

        act.Should().Throw<ArgumentException>().WithMessage("*ISO 4217*");
    }

    [Fact]
    public void MarkProcessing_FromPending_Succeeds()
    {
        var transaction = CreateTestTransaction();

        transaction.MarkProcessing();

        transaction.Status.Should().Be(TransactionStatus.Processing);
    }

    [Fact]
    public void MarkProcessing_FromCompleted_ThrowsInvalidOperationException()
    {
        var transaction = CreateTestTransaction();
        transaction.MarkCompleted();

        var act = () => transaction.MarkProcessing();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkCompleted_FromPending_Succeeds()
    {
        var transaction = CreateTestTransaction();

        transaction.MarkCompleted();

        transaction.Status.Should().Be(TransactionStatus.Completed);
        transaction.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_FromProcessing_Succeeds()
    {
        var transaction = CreateTestTransaction();
        transaction.MarkProcessing();

        transaction.MarkCompleted();

        transaction.Status.Should().Be(TransactionStatus.Completed);
    }

    [Fact]
    public void MarkFailed_FromPending_Succeeds()
    {
        var transaction = CreateTestTransaction();

        transaction.MarkFailed("Insufficient funds");

        transaction.Status.Should().Be(TransactionStatus.Failed);
        transaction.FailureReason.Should().Be("Insufficient funds");
    }

    [Fact]
    public void MarkFailed_FromCompleted_ThrowsInvalidOperationException()
    {
        var transaction = CreateTestTransaction();
        transaction.MarkCompleted();

        var act = () => transaction.MarkFailed("reason");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkReversed_FromCompleted_Succeeds()
    {
        var transaction = CreateTestTransaction();
        transaction.MarkCompleted();

        transaction.MarkReversed();

        transaction.Status.Should().Be(TransactionStatus.Reversed);
    }

    [Fact]
    public void MarkReversed_FromPending_ThrowsInvalidOperationException()
    {
        var transaction = CreateTestTransaction();

        var act = () => transaction.MarkReversed();
        act.Should().Throw<InvalidOperationException>();
    }

    private static Transaction CreateTestTransaction()
    {
        return Transaction.Create(
            idempotencyKey: Guid.NewGuid().ToString(),
            type: TransactionType.Transfer,
            sourceWalletId: Guid.NewGuid(),
            destinationWalletId: Guid.NewGuid(),
            amount: 10000,
            currency: "USD"
        );
    }
}
