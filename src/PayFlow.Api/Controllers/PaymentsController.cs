using PayFlow.Api.DTOs.Requests;
using PayFlow.Api.DTOs.Responses;
using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Transfer funds between two wallets.
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferApiRequest request,
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

        var transferRequest = new TransferRequest
        {
            IdempotencyKey = idempotencyKey,
            SourceWalletId = request.SourceWalletId,
            DestinationWalletId = request.DestinationWalletId,
            Amount = request.Amount,
            Description = request.Description ?? string.Empty
        };

        var result = await _paymentService.ProcessTransferAsync(transferRequest, cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Transfer Failed",
                Detail = result.ErrorMessage,
                Status = 422
            });

        return Ok(TransactionResponse.FromEntity(result.Transaction!));
    }
}
