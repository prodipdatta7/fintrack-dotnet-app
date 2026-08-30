using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Services;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.CreateConversation;

internal sealed class CreateConversationHandler
    : IRequestHandler<CreateConversationCommand, Result<ConversationDto>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;

    public CreateConversationHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationDto>> Handle(
        CreateConversationCommand request, CancellationToken ct)
    {
        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : !string.IsNullOrWhiteSpace(request.InitialMessage)
                ? AssistantTitleGenerator.GenerateTitle(request.InitialMessage)
                : "New Conversation";

        var conversation = new AssistantConversation
        {
            Title = title,
            IsPinned = false,
            LastMessageAt = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _conversations.InsertOneAsync(conversation, cancellationToken: ct);

        var msgCount = 0;
        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            var initialMsg = new AssistantMessage
            {
                ConversationId = conversation.Id,
                Role = "user",
                Content = request.InitialMessage.Trim(),
                UserId = _currentUser.UserId,
                CreatedBy = _currentUser.Email
            };

            await _messages.InsertOneAsync(initialMsg, cancellationToken: ct);
            msgCount = 1;
        }

        var dto = new ConversationDto(
            Id: conversation.Id,
            Title: conversation.Title,
            IsPinned: conversation.IsPinned,
            CreatedAt: conversation.CreatedAt,
            LastMessageAt: conversation.LastMessageAt,
            MessageCount: msgCount);

        return Result<ConversationDto>.Success(dto);
    }
}
