using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateAccount;

public sealed class ProposeCreateAccountValidator : AbstractValidator<ProposeCreateAccountCommand>
{
    public ProposeCreateAccountValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Account name is required.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Account type is required.");
    }
}
