using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetConversation;

internal sealed class GetConversationHandler
    : IRequestHandler<GetConversationQuery, Result<ConversationDetailDto>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;

    public GetConversationHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationDetailDto>> Handle(
        GetConversationQuery request, CancellationToken ct)
    {
        var conv = await _conversations.Find(
            Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
            Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId)
        ).FirstOrDefaultAsync(ct);

        if (conv is null)
            return Result<ConversationDetailDto>.Failure("Conversation not found or access denied.");

        var messages = await _messages.Find(
            Builders<AssistantMessage>.Filter.Eq(m => m.ConversationId, request.ConversationId) &
            Builders<AssistantMessage>.Filter.Eq(m => m.UserId, _currentUser.UserId)
        ).SortBy(m => m.CreatedAt).ToListAsync(ct);

        var messageDtos = messages.Select(m => new MessageDto(
            Id: m.Id,
            ConversationId: m.ConversationId,
            Role: m.Role,
            Content: m.Content,
            ActionType: m.ActionType,
            ActionStatus: m.ActionStatus,
            ActionSummary: m.ActionSummary,
            ActionPayloadJson: m.ActionPayloadJson,
            ToolCallJson: m.ToolCallJson,
            ToolResultJson: m.ToolResultJson,
            CreatedAt: m.CreatedAt)).ToList();

        var detail = new ConversationDetailDto(
            Id: conv.Id,
            Title: conv.Title,
            IsPinned: conv.IsPinned,
            CreatedAt: conv.CreatedAt,
            LastMessageAt: conv.LastMessageAt,
            Messages: messageDtos);

        return Result<ConversationDetailDto>.Success(detail);
    }
}
