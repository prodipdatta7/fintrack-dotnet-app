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

    [Fact]
    public async Task Handle_ShouldMapPerformedByAndDetail_AndDefaultToEmptyForLegacyEvents()
    {
        // Arrange
        var userId = "user-123";
        var transactionId = "tx-456";

        var database = Substitute.For<IMongoDatabase>();
        var collection = Substitute.For<IMongoCollection<TransactionEvent>>();
        var cursor = Substitute.For<IAsyncCursor<TransactionEvent>>();

        var storedEvents = new List<TransactionEvent>
        {
            new TransactionEvent
            {
                Id = "evt-1",
                TransactionId = transactionId,
                UserId = userId,
                EventType = "TransactionCreated",
                OccurredOnUtc = DateTime.UtcNow,
                Summary = "Transaction Created",
                DataJson = "{}",
                PerformedBy = "user@example.com",
                Detail = "Created manual record entry"
            },
            // Legacy document written before PerformedBy/Detail existed — deserializes to defaults.
            new TransactionEvent
            {
                Id = "evt-0",
                TransactionId = transactionId,
                UserId = userId,
                EventType = "TransactionCreated",
                OccurredOnUtc = DateTime.UtcNow.AddDays(-1),
                Summary = "Transaction Created",
                DataJson = "{}"
            }
        };

        cursor.Current.Returns(storedEvents);
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
        result.Value.Should().HaveCount(2);

        var enriched = result.Value!.Single(e => e.Id == "evt-1");
        enriched.PerformedBy.Should().Be("user@example.com");
        enriched.Detail.Should().Be("Created manual record entry");

        var legacy = result.Value!.Single(e => e.Id == "evt-0");
        legacy.PerformedBy.Should().Be(string.Empty);
        legacy.Detail.Should().Be(string.Empty);
    }
}
