using PayFlow.Api.DTOs.Requests;
using PayFlow.Api.DTOs.Responses;
using PayFlow.Application.DTOs;
using PayFlow.Application.Interfaces;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PayFlow.Api.Controllers;

[ApiController]
[Route("api/v1/transactions")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly PayFlowDbContext _dbContext;
    private readonly IRefundService _refundService;

    public TransactionsController(PayFlowDbContext dbContext, IRefundService refundService)
    {
        _dbContext = dbContext;
        _refundService = refundService;
    }

    /// <summary>
    /// List transactions with optional wallet filter and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTransactions(
        [FromQuery] Guid? walletId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Transactions.AsQueryable();

        if (walletId.HasValue)
        {
            query = query.Where(t => t.SourceWalletId == walletId.Value
                                  || t.DestinationWalletId == walletId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var response = new PaginatedResponse<TransactionResponse>
        {
            Items = items.Select(TransactionResponse.FromEntity),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get transaction details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(Guid id, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Transactions
            .Include(t => t.LedgerEntries)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transaction == null)
            return NotFound(new ProblemDetails
            {
                Title = "Transaction Not Found",
                Detail = $"Transaction {id} was not found.",
                Status = 404
            });

        return Ok(TransactionResponse.FromEntity(transaction));
    }

    /// <summary>
    /// Refund a completed transaction. Supports full and partial refunds.
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefundTransaction(
        Guid id,
        [FromBody] RefundApiRequest request,
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

        var refundRequest = new RefundRequest
        {
            IdempotencyKey = idempotencyKey,
            OriginalTransactionId = id,
            Amount = request.Amount,
            Reason = request.Reason
        };

        var result = await _refundService.ProcessRefundAsync(refundRequest, cancellationToken);

        if (!result.IsSuccess)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Refund Failed",
                Detail = result.ErrorMessage,
                Status = 422
            });

        return Ok(TransactionResponse.FromEntity(result.Transaction!));
    }
}
