using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.DeleteTransaction;

internal sealed class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand, Result>
{
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ICurrentUser _currentUser;

    public DeleteTransactionHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var result = await _transactions.DeleteOneAsync(
            t => t.Id == request.Id && t.UserId == _currentUser.UserId,
            cancellationToken);

        return result.DeletedCount > 0
            ? Result.Success()
            : Result.Failure("Transaction not found.");
    }
}
