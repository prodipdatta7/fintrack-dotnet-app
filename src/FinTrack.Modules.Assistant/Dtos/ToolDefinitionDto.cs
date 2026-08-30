namespace FinTrack.Modules.Assistant.Dtos;

public sealed record ToolParameterPropertyDto(
    string Type,
    string Description,
    bool IsRequired = false,
    IReadOnlyList<string>? EnumValues = null);

public sealed record ToolDefinitionDto(
    string Name,
    string Description,
    string Category, // "Read" | "ProposedWrite"
    IReadOnlyDictionary<string, ToolParameterPropertyDto> Parameters,
    IReadOnlyList<string> RequiredParameters);
