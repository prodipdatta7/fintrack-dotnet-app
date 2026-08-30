using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;

public sealed class ProposeCreateTransactionValidator : AbstractValidator<ProposeCreateTransactionCommand>
{
    public ProposeCreateTransactionValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .WithMessage("Category is required.");

        RuleFor(x => x.Account)
            .NotEmpty()
            .WithMessage("Account is required.");
    }
}
