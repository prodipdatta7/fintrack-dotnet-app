using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class GetTopSpendingExpensesHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsTopExpenseTransactionsSortedByAmountDescending()
    {
        var transactions = new List<BsonDocument>
        {
            new() { { "_id", "tx-1" }, { "UserId", TestUserId }, { "title", "Supermarket" }, { "amount", 4500m }, { "type", 2 }, { "categoryId", "cat-1" }, { "accountId", "acc-1" }, { "date", DateTime.UtcNow.AddDays(-2) }, { "note", "Weekly groceries" } },
            new() { { "_id", "tx-2" }, { "UserId", TestUserId }, { "title", "Dining Out" }, { "amount", 1800m }, { "type", 2 }, { "categoryId", "cat-1" }, { "accountId", "acc-1" }, { "date", DateTime.UtcNow.AddDays(-1) }, { "note", "Dinner with friends" } },
            new() { { "_id", "tx-3" }, { "UserId", TestUserId }, { "title", "Electricity Bill" }, { "amount", 6200m }, { "type", 2 }, { "categoryId", "cat-2" }, { "accountId", "acc-2" }, { "date", DateTime.UtcNow.AddDays(-5) }, { "note", "August bill" } }
        };

        var categories = new List<BsonDocument>
        {
            new() { { "_id", "cat-1" }, { "UserId", TestUserId }, { "Name", "Food & Dining" } },
            new() { { "_id", "cat-2" }, { "UserId", TestUserId }, { "Name", "Utilities" } }
        };

        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-1" }, { "UserId", TestUserId }, { "Name", "Cash" } },
            new() { { "_id", "acc-2" }, { "UserId", TestUserId }, { "Name", "City Bank" } }
        };

        var db = AssistantTestHelpers.MockDatabase(new()
        {
            ["transactions"] = transactions,
            ["categories"] = categories,
            ["accounts"] = accounts
        });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetTopSpendingExpensesHandler(db, currentUser);
        var result = await handler.Handle(new GetTopSpendingExpensesQuery(Period: "this_month", Limit: 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Expenses.Should().HaveCount(3);
        result.Value.TotalSpentInPeriod.Should().Be(12500m);
        result.Value.Expenses.First().CategoryName.Should().Be("Food & Dining");
    }
}
