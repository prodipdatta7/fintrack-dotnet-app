using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetMessages;

public sealed record GetMessagesQuery(
    string ConversationId,
    int Limit = 100) : IRequest<Result<IReadOnlyList<MessageDto>>>;
