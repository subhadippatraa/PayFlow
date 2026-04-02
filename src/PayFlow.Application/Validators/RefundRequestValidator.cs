using FluentValidation;
using PayFlow.Application.DTOs;

namespace PayFlow.Application.Validators;

public class RefundRequestValidator : AbstractValidator<RefundRequest>
{
    public RefundRequestValidator()
    {
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(255).WithMessage("Idempotency key must not exceed 255 characters.");

        RuleFor(x => x.OriginalTransactionId)
            .NotEmpty().WithMessage("Original transaction ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Refund amount must be positive.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.")
            .When(x => x.Reason != null);
    }
}
