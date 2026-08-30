using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProcessVoiceTurn;

public sealed record ProcessVoiceTurnCommand(
    string ConversationId,
    string Transcript,
    bool EnableTts = true
) : IRequest<Result<VoiceTurnResult>>;

public sealed record VoiceTurnResult(
    string MessageId,
    string ConversationId,
    string UserTranscript,
    string AssistantReply,
    string? ActionType,
    string? ActionStatus,
    string? ActionSummary,
    string? ActionPayloadJson,
    string? ToolName
);
