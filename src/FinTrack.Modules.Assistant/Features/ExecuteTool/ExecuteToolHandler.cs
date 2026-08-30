using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Services;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ExecuteTool;

internal sealed class ExecuteToolHandler : IRequestHandler<ExecuteToolCommand, Result<object>>
{
    private readonly IAssistantToolRegistry _toolRegistry;

    public ExecuteToolHandler(IAssistantToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public Task<Result<object>> Handle(ExecuteToolCommand request, CancellationToken ct) =>
        _toolRegistry.ExecuteToolAsync(request.ToolName, request.Arguments, ct);
}
