using FluentValidation;
using FinTrack.Modules.Categories.Features.CreateCategory;

namespace FinTrack.Modules.Categories.Features.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid category type.");
    }
}
