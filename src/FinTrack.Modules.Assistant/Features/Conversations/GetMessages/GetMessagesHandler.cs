using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.GetMessages;

internal sealed class GetMessagesHandler
    : IRequestHandler<GetMessagesQuery, Result<IReadOnlyList<MessageDto>>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;

    public GetMessagesHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(
        GetMessagesQuery request, CancellationToken ct)
    {
        var conv = await _conversations.Find(
            Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
            Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId)
        ).FirstOrDefaultAsync(ct);

        if (conv is null)
            return Result<IReadOnlyList<MessageDto>>.Failure("Conversation not found or access denied.");

        var effectiveLimit = Math.Clamp(request.Limit <= 0 ? 100 : request.Limit, 1, 200);

        var messages = await _messages.Find(
            Builders<AssistantMessage>.Filter.Eq(m => m.ConversationId, request.ConversationId) &
            Builders<AssistantMessage>.Filter.Eq(m => m.UserId, _currentUser.UserId)
        ).SortBy(m => m.CreatedAt).Limit(effectiveLimit).ToListAsync(ct);

        var dtos = messages.Select(m => new MessageDto(
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

        return Result<IReadOnlyList<MessageDto>>.Success(dtos);
    }
}
