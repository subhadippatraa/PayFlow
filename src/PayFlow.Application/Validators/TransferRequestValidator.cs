using FluentValidation;
using PayFlow.Application.DTOs;

namespace PayFlow.Application.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    public TransferRequestValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255).WithMessage("Idempotency key must not exceed 255 characters.");

        RuleFor(x => x.SourceWalletId)
            .NotEmpty().WithMessage("Source wallet ID is required.");

        RuleFor(x => x.DestinationWalletId)
            .NotEmpty().WithMessage("Destination wallet ID is required.");

        RuleFor(x => x.SourceWalletId)
            .NotEqual(x => x.DestinationWalletId)
            .WithMessage("Source and destination wallets must be different.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
