using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateCategory;

public sealed class ProposeCreateCategoryValidator : AbstractValidator<ProposeCreateCategoryCommand>
{
    public ProposeCreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required.");
    }
}
