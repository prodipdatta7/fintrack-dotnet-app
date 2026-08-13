using FluentValidation;

namespace FinTrack.Modules.Budgets.Features.CreatePlan;

public sealed class CreatePlanValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
        RuleFor(x => x.CurrentAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Deadline)
            .Must(deadline => deadline > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");
        RuleFor(x => x.Color).Matches("^#[0-9a-fA-F]{6}$");
    }
}
