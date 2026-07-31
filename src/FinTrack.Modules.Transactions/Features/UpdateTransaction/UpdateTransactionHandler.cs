using System.Globalization;
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
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly IMongoCollection<TransactionEvent> _transactionEvents;
    private readonly ICurrentUser _currentUser;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateTransactionHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        IPublishEndpoint publishEndpoint)
    {
        _database = database;
        _transactions = database.GetCollection<Transaction>("transactions");
        _transactionEvents = database.GetCollection<TransactionEvent>("transaction_events");
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
            .Set(t => t.TimeZoneOffsetInMinutes, request.TimeZoneOffsetInMinutes)
            .Set(t => t.Time, request.Time ?? string.Empty)
            .Set(t => t.PaymentMethod, request.PaymentMethod ?? string.Empty)
            .Set(t => t.ReceiptFileName, request.ReceiptFileName ?? string.Empty)
            .Set(t => t.ReceiptUrl, request.ReceiptUrl ?? string.Empty)
            .Set(t => t.Tags, request.Tags ?? string.Empty)
            .Set(t => t.Attachments, request.Attachments?.Select(a => new TransactionAttachment
            {
                FileName = a.FileName,
                FileUrl = a.FileUrl
            }).ToList() ?? new List<TransactionAttachment>())
            .Set(t => t.ModifiedAt, DateTime.UtcNow);

        var prevAmountStr = previousAmount.ToString("F2", CultureInfo.InvariantCulture);
        var newAmountStr = request.Amount.ToString("F2", CultureInfo.InvariantCulture);

        var @event = new TransactionEvent
        {
            TransactionId = request.Id,
            UserId = _currentUser.UserId,
            EventType = "TransactionUpdated",
            OccurredOnUtc = DateTime.UtcNow,
            Summary = $"Transaction updated: '{request.Title}' amount changed from ${prevAmountStr} to ${newAmountStr}",
            DataJson = System.Text.Json.JsonSerializer.Serialize(request)
        };

        try
        {
            using var session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction();

            await _transactions.UpdateOneAsync(
                session,
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                update,
                cancellationToken: cancellationToken);

            await _transactionEvents.InsertOneAsync(session, @event, cancellationToken: cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (NotSupportedException)
        {
            await _transactions.UpdateOneAsync(
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                update,
                cancellationToken: cancellationToken);

            await _transactionEvents.InsertOneAsync(@event, cancellationToken: cancellationToken);
        }

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
