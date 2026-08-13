using FluentValidation;

namespace FinTrack.Modules.Accounts.Features.UpdateAccount;

public sealed class UpdateAccountValidator : AbstractValidator<UpdateAccountCommand>
{
    private static readonly string[] AllowedTypes = ["Bank", "MFS", "Cash", "Credit"];

    public UpdateAccountValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.AccountType)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage("Account type must be one of: Bank, MFS, Cash, Credit.");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Color).Matches("^#[0-9a-fA-F]{6}$");
    }
}
