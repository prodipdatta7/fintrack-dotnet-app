using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class GetActiveAccountsSummaryHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsActiveAccountsAndCorrectSummaryTotals()
    {
        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-1" }, { "UserId", TestUserId }, { "Name", "BRAC Bank" }, { "AccountType", "Bank" }, { "Balance", 50000m }, { "Currency", "BDT" }, { "Color", "#6366f1" }, { "IsClosed", false } },
            new() { { "_id", "acc-2" }, { "UserId", TestUserId }, { "Name", "Cash" }, { "AccountType", "Cash" }, { "Balance", 3500m }, { "Currency", "BDT" }, { "Color", "#10b981" }, { "IsClosed", false } },
            new() { { "_id", "acc-3" }, { "UserId", TestUserId }, { "Name", "Old Closed" }, { "AccountType", "Bank" }, { "Balance", 0m }, { "Currency", "BDT" }, { "Color", "#9ca3af" }, { "IsClosed", true } }
        };

        var db = AssistantTestHelpers.MockDatabase(new() { ["accounts"] = accounts });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetActiveAccountsSummaryHandler(db, currentUser);
        var result = await handler.Handle(new GetActiveAccountsSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPortfolioBalance.Should().Be(53500m);
        result.Value.ActiveAccountCount.Should().Be(2);
        result.Value.Accounts.Should().HaveCount(3);
    }
}
