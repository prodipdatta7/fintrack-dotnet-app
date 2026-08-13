using FinTrack.BuildingBlocks.Auth;
using FinTrack.Contracts.Queries;
using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.CreateTransaction;
using FluentAssertions;
using MassTransit;
using MediatR;
using MongoDB.Driver;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.CreateTransaction;

public class CreateTransactionHandlerTests
{
    private const string UserId = "user-123";
    private const string Email = "user@example.com";

    [Fact]
    public async Task Handle_ShouldWriteEventWithPerformedByAndDetail_AndPersistNote()
    {
        // Arrange
        var database = Substitute.For<IMongoDatabase>();
        var transactions = Substitute.For<IMongoCollection<Transaction>>();
        var events = Substitute.For<IMongoCollection<TransactionEvent>>();

        database.GetCollection<Transaction>("transactions").Returns(transactions);
        database.GetCollection<TransactionEvent>("transaction_events").Returns(events);
        // Standalone-Mongo fallback path: session creation is not supported.
        database.Client.Throws<NotSupportedException>();

        Transaction? insertedTransaction = null;
        transactions.InsertOneAsync(
                Arg.Do<Transaction>(t => insertedTransaction = t),
                Arg.Any<InsertOneOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

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
        sender.Send(Arg.Any<ValidateCategoryExistsQuery>(), Arg.Any<CancellationToken>())
            .Returns(true);
        sender.Send(Arg.Any<ValidateAccountExistsQuery>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var publishEndpoint = Substitute.For<IPublishEndpoint>();

        var handler = new CreateTransactionHandler(database, currentUser, sender, publishEndpoint);

        var command = new CreateTransactionCommand
        {
            Title = "Grocery Shopping",
            Amount = 45.50m,
            Type = TransactionType.Expense,
            CategoryId = "cat123",
            AccountId = "acc123",
            Date = DateTime.UtcNow,
            Note = "weekly groceries"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        insertedTransaction.Should().NotBeNull();
        insertedTransaction!.Note.Should().Be("weekly groceries");

        insertedEvent.Should().NotBeNull();
        insertedEvent!.EventType.Should().Be("TransactionCreated");
        insertedEvent.PerformedBy.Should().Be(Email);
        insertedEvent.Detail.Should().Be("Created manual record entry");
    }
}
