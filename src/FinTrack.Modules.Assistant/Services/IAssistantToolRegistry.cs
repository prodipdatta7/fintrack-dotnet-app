using System.Text.Json;
using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;

namespace FinTrack.Modules.Assistant.Services;

public interface IAssistantToolRegistry
{
    IReadOnlyList<ToolDefinitionDto> GetToolDefinitions();
    Task<Result<object>> ExecuteToolAsync(string toolName, JsonElement arguments, CancellationToken ct);
}
