using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateSavingsPlan;

public sealed class ProposeCreateSavingsPlanValidator : AbstractValidator<ProposeCreateSavingsPlanCommand>
{
    public ProposeCreateSavingsPlanValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Savings plan name is required.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0)
            .WithMessage("Target amount must be greater than zero.");
    }
}
