using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.DeleteTransaction;

internal sealed class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand, Result>
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly IMongoCollection<TransactionEvent> _transactionEvents;
    private readonly ICurrentUser _currentUser;

    public DeleteTransactionHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _database = database;
        _transactions = database.GetCollection<Transaction>("transactions");
        _transactionEvents = database.GetCollection<TransactionEvent>("transaction_events");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _transactions
            .Find(t => t.Id == request.Id && t.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
            return Result.Failure("Transaction not found.");

        var amountFormatted = existing.Amount.ToString("F2", CultureInfo.InvariantCulture);
        var @event = new TransactionEvent
        {
            TransactionId = request.Id,
            UserId = _currentUser.UserId,
            EventType = "TransactionDeleted",
            OccurredOnUtc = DateTime.UtcNow,
            Summary = $"Transaction deleted: '{existing.Title}' (${amountFormatted})",
            DataJson = System.Text.Json.JsonSerializer.Serialize(existing)
        };

        try
        {
            using var session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction();

            var result = await _transactions.DeleteOneAsync(
                session,
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                cancellationToken: cancellationToken);

            if (result.DeletedCount > 0)
            {
                await _transactionEvents.InsertOneAsync(session, @event, cancellationToken: cancellationToken);
                await session.CommitTransactionAsync(cancellationToken);
                return Result.Success();
            }

            await session.AbortTransactionAsync(cancellationToken);
            return Result.Failure("Transaction not found.");
        }
        catch (NotSupportedException)
        {
            var result = await _transactions.DeleteOneAsync(
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                cancellationToken: cancellationToken);

            if (result.DeletedCount > 0)
            {
                await _transactionEvents.InsertOneAsync(@event, cancellationToken: cancellationToken);
                return Result.Success();
            }

            return Result.Failure("Transaction not found.");
        }
    }
}
