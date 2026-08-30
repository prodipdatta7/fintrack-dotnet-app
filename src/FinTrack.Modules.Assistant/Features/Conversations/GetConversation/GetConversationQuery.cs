using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetConversation;

public sealed record GetConversationQuery(
    string ConversationId) : IRequest<Result<ConversationDetailDto>>;
