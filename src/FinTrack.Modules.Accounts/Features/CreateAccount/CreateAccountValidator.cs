using FluentValidation;
using FinTrack.Modules.Accounts.Features.CreateAccount;

namespace FinTrack.Modules.Accounts.Features.CreateAccount;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid account type.");

        RuleFor(x => x.InitialBalance)
            .GreaterThanOrEqualTo(0).WithMessage("Initial balance must be non-negative.");
    }
}
