using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTag;

public sealed class ProposeCreateTagValidator : AbstractValidator<ProposeCreateTagCommand>
{
    public ProposeCreateTagValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tag name is required.");
    }
}
