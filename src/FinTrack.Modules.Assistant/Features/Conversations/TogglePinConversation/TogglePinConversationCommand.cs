using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.TogglePinConversation;

public sealed record TogglePinConversationCommand(string ConversationId) : IRequest<Result<bool>>;
