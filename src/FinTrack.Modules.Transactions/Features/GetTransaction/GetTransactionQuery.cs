using FinTrack.BuildingBlocks;
using FinTrack.Modules.Transactions.Features.GetTransactions;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.GetTransaction;

public sealed record GetTransactionQuery(string Id) : IRequest<Result<TransactionDto>>;
