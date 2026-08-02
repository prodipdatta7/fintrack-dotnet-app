using FluentValidation;

namespace FinTrack.Modules.Users.Features.DeleteAccount;

public sealed class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirmation password is required to delete account.");
    }
}
