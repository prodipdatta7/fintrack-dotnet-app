using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.UpdateTransaction;
using FluentAssertions;
using MassTransit;
using MediatR;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.UpdateTransaction;

public class UpdateTransactionHandlerTests
{
    private const string UserId = "user-123";
    private const string Email = "user@example.com";

    private static readonly DateTime FixedDate = new(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);

    private static Transaction Existing() => new()
    {
        Id = "aaaaaaaaaaaaaaaaaaaaaaaa",
        UserId = UserId,
        Title = "a",
        Amount = 120.00m,
        Type = TransactionType.Expense,
        CategoryId = "cat123",
        AccountId = "acc123",
        Date = FixedDate,
        Note = string.Empty
    };

    private static UpdateTransactionCommand CommandFrom(Transaction existing) => new()
    {
        Id = existing.Id,
        Title = existing.Title,
        Amount = existing.Amount,
        Type = existing.Type,
        CategoryId = existing.CategoryId,
        AccountId = existing.AccountId,
        Date = existing.Date,
        Note = existing.Note
    };

    [Fact]
    public void BuildChangeDetail_WithChangedAmountAndTitle_FormatsFieldLevelDiff()
    {
        // Arrange
        var existing = Existing();
        var command = new UpdateTransactionCommand
        {
            Id = existing.Id,
            Title = "b",
            Amount = 145.50m,
            Type = existing.Type,
            CategoryId = existing.CategoryId,
            AccountId = existing.AccountId,
            Date = existing.Date,
            Note = existing.Note
        };

        // Act
        var detail = UpdateTransactionHandler.BuildChangeDetail(existing, command);

        // Assert
        detail.Should().Be("Amount $120.00 → $145.50; Title 'a' → 'b'");
    }

    [Fact]
    public void BuildChangeDetail_WithChangedNote_IncludesNoteDiff()
    {
        // Arrange
        var existing = Existing();
        existing.Note = "old note";

        var changed = new UpdateTransactionCommand
        {
            Id = existing.Id,
            Title = existing.Title,
            Amount = existing.Amount,
            Type = existing.Type,
            CategoryId = existing.CategoryId,
            AccountId = existing.AccountId,
            Date = existing.Date,
            Note = "new note"
        };

        // Act
        var detail = UpdateTransactionHandler.BuildChangeDetail(existing, changed);

        // Assert
        detail.Should().Be("Note 'old note' → 'new note'");
    }

    [Fact]
    public void BuildChangeDetail_WithNoChanges_ReportsNoFieldsChanged()
    {
        // Arrange
        var existing = Existing();
        var command = CommandFrom(existing);

        // Act
        var detail = UpdateTransactionHandler.BuildChangeDetail(existing, command);

        // Assert
        detail.Should().Be("No fields changed");
    }

    [Fact]
    public async Task Handle_ShouldWriteEventWithPerformedByAndDetail()
    {
        // Arrange
        var existing = Existing();

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

        var handler = new UpdateTransactionHandler(database, currentUser, sender, publishEndpoint);

        var command = new UpdateTransactionCommand
        {
            Id = existing.Id,
            Title = "b",
            Amount = 145.50m,
            Type = existing.Type,
            CategoryId = existing.CategoryId,
            AccountId = existing.AccountId,
            Date = existing.Date,
            Note = existing.Note
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        insertedEvent.Should().NotBeNull();
        insertedEvent!.EventType.Should().Be("TransactionUpdated");
        insertedEvent.PerformedBy.Should().Be(Email);
        insertedEvent.Detail.Should().Be("Amount $120.00 → $145.50; Title 'a' → 'b'");
    }
}
