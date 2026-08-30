using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.Conversations.SendMessage;

public sealed class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("Conversation ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Message content cannot be empty.");
    }
}
