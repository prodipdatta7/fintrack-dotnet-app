using FluentValidation;

namespace FinTrack.Modules.Budgets.Features.UpdatePlan;

public sealed class UpdatePlanValidator : AbstractValidator<UpdatePlanCommand>
{
    public UpdatePlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetAmount).GreaterThan(0);
        RuleFor(x => x.CurrentAmount).GreaterThanOrEqualTo(0);
        // Deliberately weaker than CreatePlanValidator's future-deadline rule: an existing plan
        // may legitimately carry a deadline that has already passed (the user edits its title or
        // color after the date), so an update must not be rejected for a past deadline. We only
        // guard against the unset default(DateTime).
        RuleFor(x => x.Deadline)
            .Must(deadline => deadline > DateTime.MinValue)
            .WithMessage("Deadline is required.");
        RuleFor(x => x.Color).Matches("^#[0-9a-fA-F]{6}$");
    }
}
