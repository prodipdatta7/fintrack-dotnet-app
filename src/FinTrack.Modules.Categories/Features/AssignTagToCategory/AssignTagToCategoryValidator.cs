using FluentValidation;

namespace FinTrack.Modules.Categories.Features.AssignTagToCategory;

public sealed class AssignTagToCategoryValidator : AbstractValidator<AssignTagToCategoryCommand>
{
    public AssignTagToCategoryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Tag).NotEmpty().MaximumLength(60);
    }
}
