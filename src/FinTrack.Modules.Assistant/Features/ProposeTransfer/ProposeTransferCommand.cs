using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeTransfer;

public sealed record ProposeTransferCommand(
    decimal Amount,
    string FromAccount,
    string ToAccount,
    DateTime? Date = null,
    string? Note = null) : IRequest<Result<ProposedActionDto<ProposedTransferPayload>>>;
