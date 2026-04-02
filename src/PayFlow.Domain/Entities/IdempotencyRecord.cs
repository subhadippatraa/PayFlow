namespace PayFlow.Domain.Entities;

/// <summary>
/// Stores the result of an idempotent API call so that retries return the same response.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestPath { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty; // SHA-256 of request body
    public int StatusCode { get; private set; }
    public string ResponseBody { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private IdempotencyRecord() { } // EF Core

    public static IdempotencyRecord Create(
        string idempotencyKey,
        string requestPath,
        string requestHash,
        int statusCode,
        string responseBody,
        TimeSpan? ttl = null)
    {
        var expiry = ttl ?? TimeSpan.FromHours(24);

        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            RequestPath = requestPath,
            RequestHash = requestHash,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiry)
        };
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
