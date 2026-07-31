using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.GetTransactionEvents;

public sealed record GetTransactionEventsQuery(string TransactionId)
    : IRequest<Result<IReadOnlyList<TransactionEventDto>>>;
