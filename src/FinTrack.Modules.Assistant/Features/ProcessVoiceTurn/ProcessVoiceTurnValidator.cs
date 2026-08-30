using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ProcessVoiceTurn;

public sealed class ProcessVoiceTurnValidator : AbstractValidator<ProcessVoiceTurnCommand>
{
    public ProcessVoiceTurnValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(x => x.Transcript)
            .NotEmpty()
            .WithMessage("Transcript is required.")
            .MaximumLength(4000)
            .WithMessage("Transcript cannot exceed 4000 characters.");
    }
}
