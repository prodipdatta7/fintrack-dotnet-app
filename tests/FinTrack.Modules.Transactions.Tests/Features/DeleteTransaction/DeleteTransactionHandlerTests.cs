using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.DeleteTransaction;
using FluentAssertions;
using MassTransit;
using MediatR;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.DeleteTransaction;

public class DeleteTransactionHandlerTests
{
    private const string UserId = "user-123";
    private const string Email = "user@example.com";

    [Fact]
    public async Task Handle_ShouldWriteEventWithPerformedByAndDetail()
    {
        // Arrange
        var existing = new Transaction
        {
            Id = "aaaaaaaaaaaaaaaaaaaaaaaa",
            UserId = UserId,
            Title = "Grocery Shopping",
            Amount = 45.50m,
            Type = TransactionType.Expense,
            CategoryId = "cat123",
            AccountId = "acc123"
        };

        var database = Substitute.For<IMongoDatabase>();
        var transactions = Substitute.For<IMongoCollection<Transaction>>();
        var events = Substitute.For<IMongoCollection<TransactionEvent>>();

        database.GetCollection<Transaction>("transactions").Returns(transactions);
        database.GetCollection<TransactionEvent>("transaction_events").Returns(events);
        // Standalone-Mongo fallback path: session creation is not supported.
        database.Client.Throws<NotSupportedException>();

        var cursor = Substitute.For<IAsyncCursor<Transaction>>();
        cursor.Current.Returns(new List<Transaction> { existing });
        cursor.MoveNextAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true), Task.FromResult(false));

        transactions.FindAsync(
                Arg.Any<FilterDefinition<Transaction>>(),
                Arg.Any<FindOptions<Transaction, Transaction>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        transactions.DeleteOneAsync(
                Arg.Any<FilterDefinition<Transaction>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));

        transactions.DeleteOneAsync(
                Arg.Any<FilterDefinition<Transaction>>(),
                Arg.Any<DeleteOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<DeleteResult>(new DeleteResult.Acknowledged(1)));

        TransactionEvent? insertedEvent = null;
        events.InsertOneAsync(
                Arg.Do<TransactionEvent>(e => insertedEvent = e),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);
        currentUser.Email.Returns(Email);

        var sender = Substitute.For<ISender>();
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var handler = new DeleteTransactionHandler(database, currentUser, sender, publishEndpoint);

        // Act
        var result = await handler.Handle(
            new DeleteTransactionCommand(existing.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        insertedEvent.Should().NotBeNull();
        insertedEvent!.EventType.Should().Be("TransactionDeleted");
        insertedEvent.PerformedBy.Should().Be(Email);
        insertedEvent.Detail.Should().Be("Record removed from ledger");
        await publishEndpoint.Received(1).Publish(
            Arg.Is<FinTrack.Contracts.IntegrationEvents.TransactionDeleted>(e =>
                e.TransactionId == existing.Id &&
                e.AccountId == existing.AccountId &&
                e.Amount == existing.Amount &&
                e.Type == "Expense"),
            Arg.Any<CancellationToken>());
    }
}
