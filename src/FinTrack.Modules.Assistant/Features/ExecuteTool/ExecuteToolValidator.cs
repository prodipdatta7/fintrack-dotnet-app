using FluentValidation;

namespace FinTrack.Modules.Assistant.Features.ExecuteTool;

public sealed class ExecuteToolValidator : AbstractValidator<ExecuteToolCommand>
{
    public ExecuteToolValidator()
    {
        RuleFor(x => x.ToolName)
            .NotEmpty()
            .WithMessage("Tool name is required.");
    }
}
