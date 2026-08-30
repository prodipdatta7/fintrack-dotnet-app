using System.Text.Json;
using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ExecuteTool;

public sealed record ExecuteToolCommand(
    string ToolName,
    JsonElement Arguments) : IRequest<Result<object>>;
