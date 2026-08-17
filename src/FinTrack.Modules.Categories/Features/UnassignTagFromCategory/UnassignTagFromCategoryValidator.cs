using FluentValidation;

namespace FinTrack.Modules.Categories.Features.UnassignTagFromCategory;

public sealed class UnassignTagFromCategoryValidator : AbstractValidator<UnassignTagFromCategoryCommand>
{
    public UnassignTagFromCategoryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Tag).NotEmpty().MaximumLength(60);
    }
}
