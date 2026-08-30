using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.ProposeCreateTransaction;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeCreateTransactionHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsProposedActionWithResolvedCategoryAndAccount()
    {
        var categories = new List<BsonDocument>
        {
            new() { { "_id", "cat-food" }, { "UserId", TestUserId }, { "Name", "Food & Groceries" } }
        };

        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-cash" }, { "UserId", TestUserId }, { "Name", "Cash Wallet" }, { "IsClosed", false } }
        };

        var db = AssistantTestHelpers.MockDatabase(new()
        {
            ["categories"] = categories,
            ["accounts"] = accounts
        });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeCreateTransactionHandler(db, currentUser);
        var command = new ProposeCreateTransactionCommand(
            Amount: 350m,
            Category: "Food",
            Account: "Cash",
            Note: "Snacks",
            Type: "Expense");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Summary.Should().Contain("350");
        result.Value.Summary.Should().Contain("Food & Groceries");
        result.Value.Summary.Should().Contain("Cash Wallet");
        result.Value.Payload.CategoryId.Should().Be("cat-food");
        result.Value.Payload.AccountId.Should().Be("acc-cash");
        result.Value.Payload.Amount.Should().Be(350m);
    }
}
