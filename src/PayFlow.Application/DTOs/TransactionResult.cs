using PayFlow.Domain.Entities;

namespace PayFlow.Application.DTOs;

public class TransactionResult
{
    public bool IsSuccess { get; }
    public Transaction? Transaction { get; }
    public string? ErrorMessage { get; }

    private TransactionResult(bool isSuccess, Transaction? transaction, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Transaction = transaction;
        ErrorMessage = errorMessage;
    }

    public static TransactionResult Success(Transaction transaction) => new(true, transaction, null);
    public static TransactionResult Failure(string errorMessage) => new(false, null, errorMessage);
}
