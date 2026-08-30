using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class GetCategorySpendingVsBudgetHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_CalculatesSpendingAndBudgetLimitComparisonAccurately()
    {
        var categories = new List<BsonDocument>
        {
            new() { { "_id", "cat-food" }, { "UserId", TestUserId }, { "Name", "Food & Groceries" }, { "Type", 2 }, { "BudgetLimit", 15000m } }
        };

        var transactions = new List<BsonDocument>
        {
            new() { { "_id", "tx-1" }, { "UserId", TestUserId }, { "title", "Supermarket" }, { "amount", 6000m }, { "type", 2 }, { "categoryId", "cat-food" }, { "date", DateTime.UtcNow.AddDays(-1) } },
            new() { { "_id", "tx-2" }, { "UserId", TestUserId }, { "title", "Lunch" }, { "amount", 4000m }, { "type", 2 }, { "categoryId", "cat-food" }, { "date", DateTime.UtcNow.AddDays(-3) } }
        };

        var db = AssistantTestHelpers.MockDatabase(new()
        {
            ["categories"] = categories,
            ["transactions"] = transactions
        });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetCategorySpendingVsBudgetHandler(db, currentUser);
        var result = await handler.Handle(new GetCategorySpendingVsBudgetQuery("Food & Groceries", "this_month"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalSpent.Should().Be(10000m);
        result.Value.BudgetLimit.Should().Be(15000m);
        result.Value.RemainingBudget.Should().Be(5000m);
        result.Value.IsOverBudget.Should().BeFalse();
        result.Value.PercentageUsed.Should().Be(66.7m);
        result.Value.TransactionCount.Should().Be(2);
    }
}
