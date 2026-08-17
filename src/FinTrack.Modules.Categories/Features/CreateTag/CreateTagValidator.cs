using FluentValidation;

namespace FinTrack.Modules.Categories.Features.CreateTag;

public sealed class CreateTagValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
    }
}
