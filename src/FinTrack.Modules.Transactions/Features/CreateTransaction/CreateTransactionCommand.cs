using FinTrack.Modules.Transactions.Domain;
using MediatR;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

public record CreateTransactionCommand(
    string Title,
    decimal Amount,
    TransactionType Type,
    string AccountId,
    string CategoryId) : IRequest<string>;
