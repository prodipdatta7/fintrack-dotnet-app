using FluentValidation;
using FinTrack.Modules.Budgets.Features.CreateBudget;

namespace FinTrack.Modules.Budgets.Features.CreateBudget;

public sealed class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Limit).GreaterThan(0);
        RuleFor(x => x.Period).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
