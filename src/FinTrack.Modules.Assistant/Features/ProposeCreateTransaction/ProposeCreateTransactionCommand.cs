using FinTrack.BuildingBlocks;
using FinTrack.Modules.Assistant.Dtos;
using MediatR;

namespace FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;

public sealed record ProposeCreateTransactionCommand(
    decimal Amount,
    string Category,
    string Account,
    string? Note = null,
    DateTime? Date = null,
    string? Type = "Expense",
    string? Title = null) : IRequest<Result<ProposedActionDto<ProposedCreateTransactionPayload>>>;
