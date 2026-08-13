using FluentValidation;

namespace FinTrack.Modules.Users.Features.UpdateSettings;

public sealed class UpdateSettingsValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(10);

        RuleFor(x => x.TimeZone)
            .NotEmpty().WithMessage("Time zone is required.")
            .MaximumLength(50);

        RuleFor(x => x.DateFormat)
            .NotEmpty().WithMessage("Date format is required.")
            .MaximumLength(20);

        RuleFor(x => x.DefaultPageSize)
            .InclusiveBetween(5, 100).WithMessage("Default page size must be between 5 and 100.");
    }
}
