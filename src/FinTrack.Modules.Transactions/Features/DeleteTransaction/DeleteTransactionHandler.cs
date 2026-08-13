using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.Commands;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Transactions.Domain;
using MassTransit;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.DeleteTransaction;

internal sealed class DeleteTransactionHandler : IRequestHandler<DeleteTransactionCommand, Result>
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly IMongoCollection<TransactionEvent> _transactionEvents;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeleteTransactionHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        ISender sender,
        IPublishEndpoint publishEndpoint)
    {
        _database = database;
        _transactions = database.GetCollection<Transaction>("transactions");
        _transactionEvents = database.GetCollection<TransactionEvent>("transaction_events");
        _currentUser = currentUser;
        _sender = sender;
        _publishEndpoint = publishEndpoint;
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
            DataJson = System.Text.Json.JsonSerializer.Serialize(existing),
            PerformedBy = _currentUser.Email ?? string.Empty,
            Detail = "Record removed from ledger"
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
                await ApplyBalanceAndPublishAsync(existing, cancellationToken);
                return Result.Success();
            }

            await session.AbortTransactionAsync(cancellationToken);
            return Result.Failure("Transaction not found.");
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoException || ex is InvalidOperationException)
        {
            var result = await _transactions.DeleteOneAsync(
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                cancellationToken: cancellationToken);

            if (result.DeletedCount > 0)
            {
                await _transactionEvents.InsertOneAsync(@event, cancellationToken: cancellationToken);
                await ApplyBalanceAndPublishAsync(existing, cancellationToken);
                return Result.Success();
            }

            return Result.Failure("Transaction not found.");
        }
    }

    private async Task ApplyBalanceAndPublishAsync(Transaction existing, CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ApplyAccountBalanceDeltaCommand(
                existing.AccountId,
                existing.UserId,
                -SignedDelta(existing.Amount, existing.Type)),
            cancellationToken);

        await _publishEndpoint.Publish(new TransactionDeleted(
            existing.Id,
            existing.UserId,
            existing.AccountId,
            existing.Amount,
            existing.Type.ToString()), cancellationToken);
    }

    private static decimal SignedDelta(decimal amount, TransactionType type) =>
        type == TransactionType.Income ? amount : -amount;
}
