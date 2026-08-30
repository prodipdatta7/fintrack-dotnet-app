using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetConversations;

public sealed record ConversationListResult(
    IReadOnlyList<ConversationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record GetConversationsQuery(
    int Page = 1,
    int PageSize = 30,
    string? SearchTerm = null) : IRequest<Result<ConversationListResult>>;
