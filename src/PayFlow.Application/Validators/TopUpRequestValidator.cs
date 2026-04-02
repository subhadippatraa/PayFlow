using FluentValidation;
using PayFlow.Application.DTOs;

namespace PayFlow.Application.Validators;

public class TopUpRequestValidator : AbstractValidator<TopUpRequest>
{
    public TopUpRequestValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255).WithMessage("Idempotency key must not exceed 255 characters.");

        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("Wallet ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be positive.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty().WithMessage("Reference ID is required.")
            .MaximumLength(255).WithMessage("Reference ID must not exceed 255 characters.");
    }
}
