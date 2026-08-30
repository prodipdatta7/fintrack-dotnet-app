using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTag;

public sealed record ProposeCreateTagCommand(
    string Name) : IRequest<Result<ProposedActionDto<ProposedCreateTagPayload>>>;
