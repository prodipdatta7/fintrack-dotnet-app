using System.Globalization;
using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Contracts.Queries;
using FinTrack.Modules.Transactions.Domain;
using MassTransit;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

internal sealed class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, Result<string>>
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly IMongoCollection<TransactionEvent> _transactionEvents;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateTransactionHandler(
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

    public async Task<Result<string>> Handle(
        CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var categoryValid = await _sender.Send(
            new ValidateCategoryExistsQuery(request.CategoryId, _currentUser.UserId), cancellationToken);

        if (!categoryValid)
            return Result<string>.Failure("Category does not exist or does not belong to the user.");

        var transaction = new Transaction
        {
            Title = request.Title,
            Amount = request.Amount,
            Type = request.Type,
            CategoryId = request.CategoryId,
            AccountId = request.AccountId,
            Date = request.Date == default ? DateTime.UtcNow : request.Date,
            TimeZoneOffsetInMinutes = request.TimeZoneOffsetInMinutes,
            Time = request.Time ?? string.Empty,
            PaymentMethod = request.PaymentMethod ?? string.Empty,
            ReceiptFileName = request.ReceiptFileName ?? string.Empty,
            ReceiptUrl = request.ReceiptUrl ?? string.Empty,
            Tags = request.Tags ?? string.Empty,
            Attachments = request.Attachments?.Select(a => new TransactionAttachment
            {
                FileName = a.FileName,
                FileUrl = a.FileUrl
            }).ToList() ?? new List<TransactionAttachment>(),
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        var amountFormatted = transaction.Amount.ToString("F2", CultureInfo.InvariantCulture);
        var @event = new TransactionEvent
        {
            TransactionId = transaction.Id,
            UserId = _currentUser.UserId,
            EventType = "TransactionCreated",
            OccurredOnUtc = DateTime.UtcNow,
            Summary = $"Transaction created: {transaction.Title} (${amountFormatted})",
            DataJson = System.Text.Json.JsonSerializer.Serialize(transaction)
        };

        try
        {
            using var session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction();

            await _transactions.InsertOneAsync(session, transaction, cancellationToken: cancellationToken);
            await _transactionEvents.InsertOneAsync(session, @event, cancellationToken: cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (NotSupportedException)
        {
            // Standalone MongoDB instances without replica sets do not support transactions
            await _transactions.InsertOneAsync(transaction, cancellationToken: cancellationToken);
            await _transactionEvents.InsertOneAsync(@event, cancellationToken: cancellationToken);
        }

        await _publishEndpoint.Publish(new TransactionCreated(
            transaction.Id,
            transaction.UserId,
            transaction.AccountId,
            transaction.CategoryId,
            transaction.Amount,
            transaction.Type.ToString(),
            transaction.Date), cancellationToken);

        return Result<string>.Success(transaction.Id);
    }
}
