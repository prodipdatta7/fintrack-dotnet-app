using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.Conversations.UpdateConversationTitle;

public sealed class UpdateConversationTitleValidator : AbstractValidator<UpdateConversationTitleCommand>
{
    public UpdateConversationTitleValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title cannot be empty.")
            .MaximumLength(100)
            .WithMessage("Title cannot exceed 100 characters.");
    }
}
