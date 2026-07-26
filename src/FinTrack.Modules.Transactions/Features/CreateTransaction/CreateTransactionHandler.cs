using FinTrack.BuildingBlocks;
using FinTrack.Contracts.Transactions;
using FinTrack.Modules.Transactions.Domain;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Entities;

namespace FinTrack.Modules.Transactions.Features.CreateTransaction;

internal sealed class CreateTransactionHandler : IRequestHandler<CreateTransactionCommand, string>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CreateTransactionHandler> _logger;

    public CreateTransactionHandler(
        ICurrentUser currentUser,
        IPublishEndpoint publishEndpoint,
        ILogger<CreateTransactionHandler> logger)
    {
        _currentUser = currentUser;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<string> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated to create a transaction.");

        var transaction = new Domain.Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Title = request.Title,
            Amount = request.Amount,
            Type = request.Type,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow,
            TimeZoneOffsetInMinutes = 0
        };

        await transaction.SaveAsync(cancellation: cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} created for user {UserId}", transaction.ID, userId);

        await _publishEndpoint.Publish(new TransactionCreatedEvent(
            transaction.ID,
            userId,
            request.AccountId,
            request.CategoryId,
            request.Title,
            request.Amount,
            request.Type.ToString(),
            DateTime.UtcNow), cancellationToken);

        return transaction.ID;
    }
}
