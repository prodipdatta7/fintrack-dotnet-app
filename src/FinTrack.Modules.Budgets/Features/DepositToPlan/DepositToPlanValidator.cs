using FluentValidation;

namespace FinTrack.Modules.Budgets.Features.DepositToPlan;

public sealed class DepositToPlanValidator : AbstractValidator<DepositToPlanCommand>
{
    public DepositToPlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}
