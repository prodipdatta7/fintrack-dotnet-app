using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.Conversations.CreateConversation;

public sealed record CreateConversationCommand(
    string? InitialMessage = null,
    string? Title = null) : IRequest<Result<ConversationDto>>;
