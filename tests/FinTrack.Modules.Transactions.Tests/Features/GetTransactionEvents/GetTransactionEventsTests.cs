using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.GetTransactionEvents;
using FluentAssertions;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.GetTransactionEvents;

public class GetTransactionEventsTests
{
    [Fact]
    public async Task Handle_ShouldReturnEventsForTransaction()
    {
        // Arrange
        var userId = "user-123";
        var transactionId = "tx-456";

        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<TransactionEvent>>();
        var cursor = Substitute.For<IAsyncCursor<TransactionEvent>>();

        var expectedEvents = new List<TransactionEvent>
        {
            new TransactionEvent
            {
                Id = "evt-1",
                TransactionId = transactionId,
                UserId = userId,
                EventType = "TransactionCreated",
                OccurredOnUtc = DateTime.UtcNow,
                Summary = "Transaction Created",
                DataJson = "{}"
            }
        };

        cursor.Current.Returns(expectedEvents);
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));

        collection.FindAsync(
            Arg.Any<FilterDefinition<TransactionEvent>>(),
            Arg.Any<FindOptions<TransactionEvent, TransactionEvent>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        database.GetCollection<TransactionEvent>("transaction_events").Returns(collection);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(userId);

        var handler = new GetTransactionEventsHandler(database, currentUser);

        // Act
        var result = await handler.Handle(new GetTransactionEventsQuery(transactionId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
