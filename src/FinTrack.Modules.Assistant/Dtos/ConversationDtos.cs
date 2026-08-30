namespace FinTrack.Modules.Assistant.Dtos;

public sealed record ConversationDto(
    string Id,
    string Title,
    bool IsPinned,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    int MessageCount = 0);

public sealed record MessageDto(
    string Id,
    string ConversationId,
    string Role,
    string Content,
    string? ActionType,
    string? ActionStatus,
    string? ActionSummary,
    string? ActionPayloadJson,
    string? ToolCallJson,
    string? ToolResultJson,
    DateTime CreatedAt);

public sealed record ConversationDetailDto(
    string Id,
    string Title,
    bool IsPinned,
    DateTime CreatedAt,
    DateTime LastMessageAt,
    IReadOnlyList<MessageDto> Messages);
