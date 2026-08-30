using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.TogglePinConversation;

internal sealed class TogglePinConversationHandler
    : IRequestHandler<TogglePinConversationCommand, Result<bool>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly ICurrentUser _currentUser;

    public TogglePinConversationHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        TogglePinConversationCommand request, CancellationToken ct)
    {
        var filter = Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
                     Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId);

        var conv = await _conversations.Find(filter).FirstOrDefaultAsync(ct);
        if (conv is null)
        {
            return Result<bool>.Failure("Conversation was not found.");
        }

        var newPinnedState = !conv.IsPinned;
        var update = Builders<AssistantConversation>.Update
            .Set(c => c.IsPinned, newPinnedState)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        await _conversations.UpdateOneAsync(filter, update, cancellationToken: ct);

        return Result<bool>.Success(newPinnedState);
    }
}
