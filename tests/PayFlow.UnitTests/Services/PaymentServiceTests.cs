using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Events;
using PayFlow.Domain.Exceptions;
using PayFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace PayFlow.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILedgerService> _ledgerServiceMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _ledgerServiceMock = new Mock<ILedgerService>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _loggerMock = new Mock<ILogger<PaymentService>>();

        _sut = new PaymentService(
            _unitOfWorkMock.Object,
            _ledgerServiceMock.Object,
            _eventPublisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessTransferAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var sourceWallet = Wallet.Create(Guid.NewGuid(), "USD");
        sourceWallet.Credit(100000); // $1000

        var destWallet = Wallet.Create(Guid.NewGuid(), "USD");

        var request = new TransferRequest
        {
            IdempotencyKey = "transfer_123",
            SourceWalletId = sourceWallet.Id,
            DestinationWalletId = destWallet.Id,
            Amount = 50000, // $500
            Description = "Test transfer"
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(sourceWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWallet);
        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(destWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destWallet);

        var txnRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(txnRepoMock.Object);

        // Act
        var result = await _sut.ProcessTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Transaction.Should().NotBeNull();
        result.Transaction!.Type.Should().Be(TransactionType.Transfer);
        result.Transaction.Status.Should().Be(TransactionStatus.Completed);
        result.Transaction.Amount.Should().Be(50000);

        sourceWallet.Balance.Should().Be(50000); // $500 remaining
        destWallet.Balance.Should().Be(50000); // $500 received

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(
            e => e.PublishAsync(It.IsAny<PaymentCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransferAsync_WalletNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new TransferRequest
        {
            IdempotencyKey = "transfer_404",
            SourceWalletId = Guid.NewGuid(),
            DestinationWalletId = Guid.NewGuid(),
            Amount = 10000
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var result = await _sut.ProcessTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");

        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTransferAsync_CurrencyMismatch_ReturnsFailure()
    {
        // Arrange
        var sourceWallet = Wallet.Create(Guid.NewGuid(), "USD");
        sourceWallet.Credit(100000);

        var destWallet = Wallet.Create(Guid.NewGuid(), "INR");

        var request = new TransferRequest
        {
            IdempotencyKey = "transfer_curr",
            SourceWalletId = sourceWallet.Id,
            DestinationWalletId = destWallet.Id,
            Amount = 50000
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(sourceWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWallet);
        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(destWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destWallet);

        // Act
        var result = await _sut.ProcessTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Currency mismatch");
    }

    [Fact]
    public async Task ProcessTransferAsync_InsufficientFunds_ReturnsFailure()
    {
        // Arrange
        var sourceWallet = Wallet.Create(Guid.NewGuid(), "USD");
        sourceWallet.Credit(1000); // Only $10

        var destWallet = Wallet.Create(Guid.NewGuid(), "USD");

        var request = new TransferRequest
        {
            IdempotencyKey = "transfer_insuf",
            SourceWalletId = sourceWallet.Id,
            DestinationWalletId = destWallet.Id,
            Amount = 50000 // $500 — exceeds balance
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(sourceWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWallet);
        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(destWallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destWallet);

        var txnRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(txnRepoMock.Object);

        // Act
        var result = await _sut.ProcessTransferAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTopUpAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        var request = new TopUpRequest
        {
            IdempotencyKey = "topup_123",
            WalletId = wallet.Id,
            Amount = 100000,
            ReferenceId = "ref_001"
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(wallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var txnRepoMock = new Mock<ITransactionRepository>();
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(txnRepoMock.Object);

        // Act
        var result = await _sut.ProcessTopUpAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Transaction.Should().NotBeNull();
        result.Transaction!.Type.Should().Be(TransactionType.TopUp);
        wallet.Balance.Should().Be(100000);

        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTopUpAsync_WalletNotFound_ReturnsFailure()
    {
        var request = new TopUpRequest
        {
            IdempotencyKey = "topup_404",
            WalletId = Guid.NewGuid(),
            Amount = 10000,
            ReferenceId = "ref_002"
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        var result = await _sut.ProcessTopUpAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Wallet not found");
    }
}
