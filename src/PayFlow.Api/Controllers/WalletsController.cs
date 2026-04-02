using PayFlow.Api.DTOs.Requests;
using PayFlow.Api.DTOs.Responses;
using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/wallets")]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IPaymentService _paymentService;

    public WalletsController(IWalletService walletService, IPaymentService paymentService)
    {
        _walletService = walletService;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Create a new wallet for a user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateWallet(
        [FromBody] CreateWalletApiRequest request,
        CancellationToken cancellationToken)
    {
        var walletRequest = new CreateWalletRequest
        {
            UserId = request.UserId,
            Currency = request.Currency
        };

        var wallet = await _walletService.CreateWalletAsync(walletRequest, cancellationToken);
        var response = WalletResponse.FromEntity(wallet);

        return CreatedAtAction(nameof(GetWallet), new { id = wallet.Id }, response);
    }

    /// <summary>
    /// Get wallet details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWallet(Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetWalletAsync(id, cancellationToken);

        if (wallet == null)
            return NotFound(new ProblemDetails
            {
                Type = "https://payflow.dev/errors/wallet-not-found",
                Title = "Wallet Not Found",
                Status = 404,
                Detail = $"Wallet {id} was not found.",
                Instance = Request.Path
            });

        return Ok(WalletResponse.FromEntity(wallet));
    }

    /// <summary>
    /// Top up a wallet.
    /// </summary>
    [HttpPost("{id:guid}/topup")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TopUpWallet(
        Guid id,
        [FromBody] TopUpApiRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new ProblemDetails
            {
                Title = "Missing Header",
                Detail = "Idempotency-Key header is required.",
                Status = 400
            });

        var topUpRequest = new TopUpRequest
        {
            IdempotencyKey = idempotencyKey,
            WalletId = id,
            Amount = request.Amount,
            ReferenceId = request.ReferenceId
        };

        var result = await _paymentService.ProcessTopUpAsync(topUpRequest, cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Top-Up Failed",
                Detail = result.ErrorMessage,
                Status = 422
            });

        return Ok(TransactionResponse.FromEntity(result.Transaction!));
    }
}
