using PayFlow.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace PayFlow.Api.Middleware;

/// <summary>
/// Global exception handler that converts domain exceptions to RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            InsufficientFundsException ex => (HttpStatusCode.UnprocessableEntity, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/insufficient-funds",
                Title = "Insufficient Funds",
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            WalletNotFoundException ex => (HttpStatusCode.NotFound, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/wallet-not-found",
                Title = "Wallet Not Found",
                Status = (int)HttpStatusCode.NotFound,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            DuplicateTransactionException ex => (HttpStatusCode.Conflict, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/duplicate-transaction",
                Title = "Duplicate Transaction",
                Status = (int)HttpStatusCode.Conflict,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            ConcurrencyConflictException ex => (HttpStatusCode.Conflict, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/concurrency-conflict",
                Title = "Concurrency Conflict",
                Status = (int)HttpStatusCode.Conflict,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            CurrencyMismatchException ex => (HttpStatusCode.UnprocessableEntity, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/currency-mismatch",
                Title = "Currency Mismatch",
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            ArgumentException ex => (HttpStatusCode.BadRequest, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/bad-request",
                Title = "Bad Request",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            InvalidOperationException ex => (HttpStatusCode.UnprocessableEntity, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/invalid-operation",
                Title = "Invalid Operation",
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Detail = ex.Message,
                Instance = context.Request.Path
            }),

            _ => (HttpStatusCode.InternalServerError, new ProblemDetails
            {
                Type = "https://payflow.dev/errors/internal",
                Title = "Internal Server Error",
                Status = (int)HttpStatusCode.InternalServerError,
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path
            })
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled domain exception {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
        }

        // Add trace ID
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
