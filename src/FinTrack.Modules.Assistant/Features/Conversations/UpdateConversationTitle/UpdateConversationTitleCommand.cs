using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.UpdateConversationTitle;

public sealed record UpdateConversationTitleCommand(
    string ConversationId,
    string Title) : IRequest<Result<string>>;
