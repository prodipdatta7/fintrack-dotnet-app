using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.DeleteConversation;

public sealed record DeleteConversationCommand(
    string ConversationId) : IRequest<Result>;
