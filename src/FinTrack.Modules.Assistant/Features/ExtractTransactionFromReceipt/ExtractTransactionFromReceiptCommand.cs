using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ExtractTransactionFromReceipt;

public sealed record ExtractTransactionFromReceiptCommand(
    string? ImageBase64 = null,
    string? FileName = null,
    string? RawText = null) : IRequest<Result<ProposedActionDto<ProposedCreateTransactionPayload>>>;
