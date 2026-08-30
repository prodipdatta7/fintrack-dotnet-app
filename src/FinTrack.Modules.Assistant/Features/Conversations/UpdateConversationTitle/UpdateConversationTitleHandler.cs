using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.UpdateConversationTitle;

internal sealed class UpdateConversationTitleHandler
    : IRequestHandler<UpdateConversationTitleCommand, Result<string>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly ICurrentUser _currentUser;

    public UpdateConversationTitleHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(
        UpdateConversationTitleCommand request, CancellationToken ct)
    {
        var filter = Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
                     Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId);

        var update = Builders<AssistantConversation>.Update
            .Set(c => c.Title, request.Title.Trim())
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        var result = await _conversations.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
            return Result<string>.Failure("Conversation not found or access denied.");

        return Result<string>.Success(request.Title.Trim());
    }
}
