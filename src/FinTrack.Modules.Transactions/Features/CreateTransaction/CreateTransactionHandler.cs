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
    private readonly IMongoCollection<Transaction> _transactions;
    private readonly ICurrentUser _currentUser;
    private readonly ISender _sender;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateTransactionHandler(
        IMongoDatabase database,
        ICurrentUser currentUser,
        ISender sender,
        IPublishEndpoint publishEndpoint)
    {
        _transactions = database.GetCollection<Transaction>("transactions");
        _currentUser = currentUser;
        _sender = sender;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<string>> Handle(
        CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        // Cross-module validation via synchronous query contract
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
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _transactions.InsertOneAsync(transaction, cancellationToken: cancellationToken);

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
