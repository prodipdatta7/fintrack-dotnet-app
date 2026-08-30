using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Services;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetToolSchemas;

internal sealed class GetToolSchemasHandler : IRequestHandler<GetToolSchemasQuery, Result<IReadOnlyList<ToolDefinitionDto>>>
{
    private readonly IAssistantToolRegistry _toolRegistry;

    public GetToolSchemasHandler(IAssistantToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    public Task<Result<IReadOnlyList<ToolDefinitionDto>>> Handle(GetToolSchemasQuery request, CancellationToken ct)
    {
        var definitions = _toolRegistry.GetToolDefinitions();
        return Task.FromResult(Result<IReadOnlyList<ToolDefinitionDto>>.Success(definitions));
    }
}
