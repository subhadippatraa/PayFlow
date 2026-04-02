namespace PayFlow.Domain.Exceptions;

public class DuplicateTransactionException : Exception
{
    public string IdempotencyKey { get; }

    public DuplicateTransactionException(string idempotencyKey)
        : base($"A transaction with idempotency key '{idempotencyKey}' already exists.")
    {
        IdempotencyKey = idempotencyKey;
    }
}
