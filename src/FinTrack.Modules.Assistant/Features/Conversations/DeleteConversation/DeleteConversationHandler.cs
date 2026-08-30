using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.DeleteConversation;

internal sealed class DeleteConversationHandler
    : IRequestHandler<DeleteConversationCommand, Result>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;

    public DeleteConversationHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteConversationCommand request, CancellationToken ct)
    {
        var convFilter = Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
                         Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId);

        var deleteResult = await _conversations.DeleteOneAsync(convFilter, ct);

        if (deleteResult.DeletedCount == 0)
            return Result.Failure("Conversation not found or access denied.");

        var msgFilter = Builders<AssistantMessage>.Filter.Eq(m => m.ConversationId, request.ConversationId) &
                        Builders<AssistantMessage>.Filter.Eq(m => m.UserId, _currentUser.UserId);

        await _messages.DeleteManyAsync(msgFilter, ct);

        return Result.Success();
    }
}
