using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.DeleteTransaction;

public sealed record DeleteTransactionCommand(string Id) : IRequest<Result>;
