using FluentValidation;

namespace FinTrack.Modules.Accounts.Features.UpdateAccountBalance;

public sealed class UpdateAccountBalanceValidator : AbstractValidator<UpdateAccountBalanceCommand>
{
    public UpdateAccountBalanceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Balance).GreaterThanOrEqualTo(0);
    }
}
