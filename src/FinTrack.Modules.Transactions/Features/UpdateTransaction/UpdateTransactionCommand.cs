using FinTrack.BuildingBlocks;
using FinTrack.Modules.Transactions.Domain;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.UpdateTransaction;

public sealed record UpdateTransactionCommand(
    string Id,
    string Title,
    decimal Amount,
    TransactionType Type,
    string CategoryId,
    string AccountId,
    DateTime Date) : IRequest<Result>;
