using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;

namespace FinTrack.Modules.Assistant.Services.Ai;

public record GeminiTurnResult(
    bool IsSuccess,
    string? ReplyText = null,
    string? FunctionCallName = null,
    string? FunctionCallArgsJson = null,
    string? Error = null
);

public interface IGeminiAiService
{
    bool IsConfigured { get; }
    Task<GeminiTurnResult> ProcessTurnAsync(
        string userPrompt,
        IReadOnlyList<AssistantMessage> conversationHistory,
        IReadOnlyList<ToolDefinitionDto> tools,
        CancellationToken ct = default);
}
