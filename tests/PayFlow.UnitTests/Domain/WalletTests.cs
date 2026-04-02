using PayFlow.Domain.Entities;
using PayFlow.Domain.Exceptions;
using FluentAssertions;

namespace PayFlow.UnitTests.Domain;

public class WalletTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsWallet()
    {
        var userId = Guid.NewGuid();
        var wallet = Wallet.Create(userId, "USD");

        wallet.Id.Should().NotBeEmpty();
        wallet.UserId.Should().Be(userId);
        wallet.Currency.Should().Be("USD");
        wallet.Balance.Should().Be(0);
        wallet.HeldBalance.Should().Be(0);
        wallet.AvailableBalance.Should().Be(0);
        wallet.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_InvalidCurrency_ThrowsArgumentException()
    {
        var act = () => Wallet.Create(Guid.NewGuid(), "US");
        act.Should().Throw<ArgumentException>().WithMessage("*ISO 4217*");
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsArgumentException()
    {
        var act = () => Wallet.Create(Guid.Empty, "USD");
        act.Should().Throw<ArgumentException>().WithMessage("*User ID*");
    }

    [Fact]
    public void Credit_ValidAmount_IncreasesBalance()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        wallet.Credit(10000);

        wallet.Balance.Should().Be(10000);
        wallet.AvailableBalance.Should().Be(10000);
    }

    [Fact]
    public void Credit_ZeroAmount_ThrowsArgumentException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        var act = () => wallet.Credit(0);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void Debit_ValidAmount_DecreasesBalance()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(10000);

        wallet.Debit(3000);

        wallet.Balance.Should().Be(7000);
    }

    [Fact]
    public void Debit_InsufficientFunds_ThrowsInsufficientFundsException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(5000);

        var act = () => wallet.Debit(10000);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void Debit_ZeroAmount_ThrowsArgumentException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        var act = () => wallet.Debit(0);
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void HoldFunds_ValidAmount_IncreasesHeldBalance()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(10000);

        wallet.HoldFunds(3000);

        wallet.HeldBalance.Should().Be(3000);
        wallet.AvailableBalance.Should().Be(7000);
        wallet.Balance.Should().Be(10000); // Balance unchanged
    }

    [Fact]
    public void HoldFunds_ExceedsAvailable_ThrowsInsufficientFundsException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(5000);
        wallet.HoldFunds(3000);

        // Available is now 2000, try to hold 3000 more
        var act = () => wallet.HoldFunds(3000);
        act.Should().Throw<InsufficientFundsException>();
    }

    [Fact]
    public void ReleaseFunds_ValidAmount_DecreasesHeldBalance()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(10000);
        wallet.HoldFunds(5000);

        wallet.ReleaseFunds(2000);

        wallet.HeldBalance.Should().Be(3000);
        wallet.AvailableBalance.Should().Be(7000);
    }

    [Fact]
    public void ReleaseFunds_ExceedsHeld_ThrowsInvalidOperationException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(10000);
        wallet.HoldFunds(3000);

        var act = () => wallet.ReleaseFunds(5000);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deactivate_ZeroBalance_Succeeds()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        wallet.Deactivate();

        wallet.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_PositiveBalance_ThrowsInvalidOperationException()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");
        wallet.Credit(100);

        var act = () => wallet.Deactivate();
        act.Should().Throw<InvalidOperationException>();
    }
}
