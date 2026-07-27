using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Transactions.Domain;
using MassTransit;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.UpdateTransaction;

internal sealed class UpdateTransactionHandler : IRequestHandler<UpdateTransactionCommand, Result>
{
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ICurrentUser _currentUser;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateTransactionHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IPublishEndpoint publishEndpoint)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _currentUser = currentUser;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result> Handle(
        UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _transactions
            .Find(t => t.Id == request.Id && t.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
            return Result.Failure("Transaction not found.");

        var previousAmount = existing.Amount;

        var update = Builders<Transaction>.Update
            .Set(t => t.Title, request.Title)
            .Set(t => t.Amount, request.Amount)
            .Set(t => t.Type, request.Type)
            .Set(t => t.CategoryId, request.CategoryId)
            .Set(t => t.AccountId, request.AccountId)
            .Set(t => t.Date, request.Date)
            .Set(t => t.ModifiedAt, DateTime.UtcNow);

        await _transactions.UpdateOneAsync(
            t => t.Id == request.Id && t.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        await _publishEndpoint.Publish(new TransactionUpdated(
            request.Id,
            _currentUser.UserId,
            request.AccountId,
            request.CategoryId,
            request.Amount,
            previousAmount,
            request.Type.ToString(),
            request.Date), cancellationToken);

        return Result.Success();
    }
}
