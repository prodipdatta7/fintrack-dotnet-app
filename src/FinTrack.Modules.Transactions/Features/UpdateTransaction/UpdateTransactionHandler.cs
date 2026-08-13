using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.Commands;
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
    private readonly ISender _sender;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateTransactionHandler(
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
        UpdateTransactionCommand request, CancellationToken cancellationToken)
    {
        var existing = await _transactions
            .Find(t => t.Id == request.Id && t.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
            return Result.Failure("Transaction not found.");

        var previousAmount = existing.Amount;
        var previousAccountId = existing.AccountId;
        var previousType = existing.Type;

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
            .Set(t => t.Note, request.Note ?? string.Empty)
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
            DataJson = System.Text.Json.JsonSerializer.Serialize(request),
            PerformedBy = _currentUser.Email ?? string.Empty,
            Detail = BuildChangeDetail(existing, request)
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
        catch (Exception ex) when (ex is NotSupportedException || ex is MongoException || ex is InvalidOperationException)
        {
            await _transactions.UpdateOneAsync(
                t => t.Id == request.Id && t.UserId == _currentUser.UserId,
                update,
                cancellationToken: cancellationToken);

            await _transactionEvents.InsertOneAsync(@event, cancellationToken: cancellationToken);
        }

        // Same account → one net delta (avoids double-counting when an unsynced backfill runs).
        // Different accounts → reverse the old effect, then apply the new one.
        if (string.Equals(previousAccountId, request.AccountId, StringComparison.Ordinal))
        {
            var netDelta = SignedDelta(request.Amount, request.Type) - SignedDelta(previousAmount, previousType);
            await _sender.Send(
                new ApplyAccountBalanceDeltaCommand(request.AccountId, _currentUser.UserId, netDelta),
                cancellationToken);
        }
        else
        {
            await _sender.Send(
                new ApplyAccountBalanceDeltaCommand(
                    previousAccountId,
                    _currentUser.UserId,
                    -SignedDelta(previousAmount, previousType)),
                cancellationToken);
            await _sender.Send(
                new ApplyAccountBalanceDeltaCommand(
                    request.AccountId,
                    _currentUser.UserId,
                    SignedDelta(request.Amount, request.Type)),
                cancellationToken);
        }

        await _publishEndpoint.Publish(new TransactionUpdated(
            request.Id,
            _currentUser.UserId,
            request.AccountId,
            request.CategoryId,
            request.Amount,
            previousAmount,
            request.Type.ToString(),
            request.Date,
            previousAccountId,
            previousType.ToString()), cancellationToken);

        return Result.Success();
    }

    private static decimal SignedDelta(decimal amount, TransactionType type) =>
        type == TransactionType.Income ? amount : -amount;

    /// <summary>
    /// Builds a human-readable, field-level diff containing only the fields that changed,
    /// e.g. "Amount $120.00 → $145.50; Title 'a' → 'b'".
    /// </summary>
    internal static string BuildChangeDetail(Transaction existing, UpdateTransactionCommand request)
    {
        var changes = new List<string>();

        if (existing.Amount != request.Amount)
        {
            var from = existing.Amount.ToString("F2", CultureInfo.InvariantCulture);
            var to = request.Amount.ToString("F2", CultureInfo.InvariantCulture);
            changes.Add($"Amount ${from} → ${to}");
        }

        if (!string.Equals(existing.Title, request.Title, StringComparison.Ordinal))
            changes.Add($"Title '{existing.Title}' → '{request.Title}'");

        if (existing.Type != request.Type)
            changes.Add($"Type {existing.Type} → {request.Type}");

        if (!string.Equals(existing.CategoryId, request.CategoryId, StringComparison.Ordinal))
            changes.Add($"Category '{existing.CategoryId}' → '{request.CategoryId}'");

        if (!string.Equals(existing.AccountId, request.AccountId, StringComparison.Ordinal))
            changes.Add($"Account '{existing.AccountId}' → '{request.AccountId}'");

        if (existing.Date != request.Date)
            changes.Add($"Date {existing.Date:yyyy-MM-dd} → {request.Date:yyyy-MM-dd}");

        var newNote = request.Note ?? string.Empty;
        if (!string.Equals(existing.Note ?? string.Empty, newNote, StringComparison.Ordinal))
            changes.Add($"Note '{existing.Note}' → '{newNote}'");

        var newPaymentMethod = request.PaymentMethod ?? string.Empty;
        if (!string.Equals(existing.PaymentMethod, newPaymentMethod, StringComparison.Ordinal))
            changes.Add($"Payment method '{existing.PaymentMethod}' → '{newPaymentMethod}'");

        var newTags = request.Tags ?? string.Empty;
        if (!string.Equals(existing.Tags, newTags, StringComparison.Ordinal))
            changes.Add($"Tags '{existing.Tags}' → '{newTags}'");

        return changes.Count == 0 ? "No fields changed" : string.Join("; ", changes);
    }
}
