using FinTrack.BuildingBlocks;
using FinTrack.Modules.Transactions.Domain;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

public sealed record CreateTransactionCommand(
    string Title,
    decimal Amount,
    TransactionType Type,
    string CategoryId,
    string AccountId,
    DateTime Date,
    int TimeZoneOffsetInMinutes) : IRequest<Result<string>>;
