using PayFlow.Domain.Entities;
using PayFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace PayFlow.Api.Middleware;

/// <summary>
/// Middleware that enforces idempotency for POST/PUT/PATCH requests.
/// Clients must send an Idempotency-Key header. If the key was seen before
/// (and hasn't expired), the cached response is returned without re-executing.
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply to mutating methods
        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check for Idempotency-Key header
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKey)
            || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<PayFlowDbContext>();
        var requestPath = context.Request.Path.ToString();
        var requestHash = await ComputeRequestHashAsync(context.Request);

        // Check for existing record
        var existingRecord = await dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey.ToString());

        if (existingRecord != null)
        {
            if (existingRecord.IsExpired())
            {
                // Expired record — remove and proceed
                dbContext.IdempotencyRecords.Remove(existingRecord);
                await dbContext.SaveChangesAsync();
            }
            else if (existingRecord.RequestHash != requestHash)
            {
                // Same key, different body — misuse
                context.Response.StatusCode = 422;
                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://payflow.dev/errors/idempotency-mismatch",
                    title = "Idempotency Key Mismatch",
                    status = 422,
                    detail = "The idempotency key was used with a different request body."
                });
                return;
            }
            else
            {
                // Return cached response
                _logger.LogInformation("Returning cached idempotent response for key {Key}", idempotencyKey);
                context.Response.StatusCode = existingRecord.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(existingRecord.ResponseBody);
                return;
            }
        }

        // Capture the response
        var originalResponseBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Read the response and store idempotency record
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);

        // Only cache successful responses (2xx)
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            var record = IdempotencyRecord.Create(
                idempotencyKey: idempotencyKey.ToString()!,
                requestPath: requestPath,
                requestHash: requestHash,
                statusCode: context.Response.StatusCode,
                responseBody: responseText
            );

            await dbContext.IdempotencyRecords.AddAsync(record);
            await dbContext.SaveChangesAsync();
        }

        await responseBody.CopyToAsync(originalResponseBody);
        context.Response.Body = originalResponseBody;
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request)
    {
        request.EnableBuffering();
        request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
