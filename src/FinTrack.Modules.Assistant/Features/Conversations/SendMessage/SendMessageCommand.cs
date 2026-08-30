using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.SendMessage;

public sealed record SendMessageCommand(
    string ConversationId,
    string Content,
    string? Role = "user",
    string? ActionType = null,
    string? ActionStatus = null,
    string? ActionSummary = null,
    string? ActionPayloadJson = null,
    string? ToolCallJson = null,
    string? ToolResultJson = null) : IRequest<Result<MessageDto>>;
