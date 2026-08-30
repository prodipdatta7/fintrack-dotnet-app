using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.GetToolSchemas;

public sealed record GetToolSchemasQuery : IRequest<Result<IReadOnlyList<ToolDefinitionDto>>>;
