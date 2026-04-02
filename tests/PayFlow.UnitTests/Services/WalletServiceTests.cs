using PayFlow.Application.DTOs;
using PayFlow.Application.Services;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace PayFlow.UnitTests.Services;

public class WalletServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<WalletService>> _loggerMock;
    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<WalletService>>();
        _sut = new WalletService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateWalletAsync_ValidRequest_ReturnsWallet()
    {
        // Arrange
        var request = new CreateWalletRequest
        {
            UserId = Guid.NewGuid(),
            Currency = "USD"
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByUserIdAndCurrencyAsync(
                request.UserId, request.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        var walletRepoMock = new Mock<IWalletRepository>();
        _unitOfWorkMock.Setup(u => u.Wallets).Returns(walletRepoMock.Object);

        walletRepoMock.Setup(w => w.GetByUserIdAndCurrencyAsync(
                request.UserId, request.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var wallet = await _sut.CreateWalletAsync(request);

        // Assert
        wallet.Should().NotBeNull();
        wallet.UserId.Should().Be(request.UserId);
        wallet.Currency.Should().Be("USD");
        wallet.Balance.Should().Be(0);

        walletRepoMock.Verify(w => w.AddAsync(It.IsAny<Wallet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWalletAsync_DuplicateWallet_ThrowsInvalidOperationException()
    {
        var existingWallet = Wallet.Create(Guid.NewGuid(), "USD");

        var request = new CreateWalletRequest
        {
            UserId = existingWallet.UserId,
            Currency = "USD"
        };

        _unitOfWorkMock.Setup(u => u.Wallets.GetByUserIdAndCurrencyAsync(
                request.UserId, request.Currency, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWallet);

        var act = async () => await _sut.CreateWalletAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has*");
    }

    [Fact]
    public async Task GetWalletAsync_ExistingId_ReturnsWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "USD");

        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(wallet.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var result = await _sut.GetWalletAsync(wallet.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(wallet.Id);
    }

    [Fact]
    public async Task GetWalletAsync_NonExistingId_ReturnsNull()
    {
        _unitOfWorkMock.Setup(u => u.Wallets.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        var result = await _sut.GetWalletAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}
