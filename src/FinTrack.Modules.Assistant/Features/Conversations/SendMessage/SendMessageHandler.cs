using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Services;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.Conversations.SendMessage;

internal sealed class SendMessageHandler
    : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private readonly IMongoCollection<AssistantConversation> _conversations;
    private readonly IMongoCollection<AssistantMessage> _messages;
    private readonly ICurrentUser _currentUser;
    private readonly IAssistantGuardrailsService _guardrails;

    public SendMessageHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IAssistantGuardrailsService guardrails)
    {
        _conversations = database.GetCollection<AssistantConversation>("assistant_conversations");
        _messages = database.GetCollection<AssistantMessage>("assistant_messages");
        _currentUser = currentUser;
        _guardrails = guardrails;
    }

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request, CancellationToken ct)
    {
        // 1. Enforce Rate Limiting
        var rateLimitCheck = _guardrails.CheckRateLimit(_currentUser.UserId);
        if (!rateLimitCheck.IsSuccess)
            return Result<MessageDto>.Failure(rateLimitCheck.Error);

        // 2. Validate and Sanitize User Content
        var contentValidation = _guardrails.SanitizeAndValidateInput(request.Content);
        if (!contentValidation.IsSuccess)
            return Result<MessageDto>.Failure(contentValidation.Error);

        var sanitizedContent = contentValidation.Value!;

        var conv = await _conversations.Find(
            Builders<AssistantConversation>.Filter.Eq(c => c.Id, request.ConversationId) &
            Builders<AssistantConversation>.Filter.Eq(c => c.UserId, _currentUser.UserId)
        ).FirstOrDefaultAsync(ct);

        if (conv is null)
            return Result<MessageDto>.Failure("Conversation not found or access denied.");

        var message = new AssistantMessage
        {
            ConversationId = request.ConversationId,
            Role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role.Trim(),
            Content = sanitizedContent,
            ActionType = request.ActionType,
            ActionStatus = request.ActionStatus,
            ActionSummary = request.ActionSummary,
            ActionPayloadJson = request.ActionPayloadJson,
            ToolCallJson = request.ToolCallJson,
            ToolResultJson = request.ToolResultJson,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _messages.InsertOneAsync(message, cancellationToken: ct);

        // Update conversation LastMessageAt and auto-generate title if needed
        var updateBuilder = Builders<AssistantConversation>.Update
            .Set(c => c.LastMessageAt, DateTime.UtcNow)
            .Set(c => c.ModifiedAt, DateTime.UtcNow);

        if (conv.Title == "New Conversation" && message.Role == "user")
        {
            var newTitle = AssistantTitleGenerator.GenerateTitle(message.Content);
            updateBuilder = updateBuilder.Set(c => c.Title, newTitle);
        }

        await _conversations.UpdateOneAsync(
            Builders<AssistantConversation>.Filter.Eq(c => c.Id, conv.Id),
            updateBuilder,
            cancellationToken: ct);

        var dto = new MessageDto(
            Id: message.Id,
            ConversationId: message.ConversationId,
            Role: message.Role,
            Content: message.Content,
            ActionType: message.ActionType,
            ActionStatus: message.ActionStatus,
            ActionSummary: message.ActionSummary,
            ActionPayloadJson: message.ActionPayloadJson,
            ToolCallJson: message.ToolCallJson,
            ToolResultJson: message.ToolResultJson,
            CreatedAt: message.CreatedAt);

        return Result<MessageDto>.Success(dto);
    }
}
