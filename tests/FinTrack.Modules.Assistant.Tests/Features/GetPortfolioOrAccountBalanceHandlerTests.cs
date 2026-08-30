using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class GetPortfolioOrAccountBalanceHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_WithoutParameters_ReturnsTotalPortfolioBalanceAcrossActiveAccounts()
    {
        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-1" }, { "UserId", TestUserId }, { "Name", "Cash" }, { "AccountType", "Cash" }, { "Balance", 5000m }, { "Currency", "BDT" }, { "IsClosed", false } },
            new() { { "_id", "acc-2" }, { "UserId", TestUserId }, { "Name", "bKash" }, { "AccountType", "MFS" }, { "Balance", 12000m }, { "Currency", "BDT" }, { "IsClosed", false } }
        };

        var db = AssistantTestHelpers.MockDatabase(new() { ["accounts"] = accounts });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetPortfolioOrAccountBalanceHandler(db, currentUser);
        var result = await handler.Handle(new GetPortfolioOrAccountBalanceQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalBalance.Should().Be(17000m);
        result.Value.AccountCount.Should().Be(2);
        result.Value.TargetAccount.Should().BeNull();
        result.Value.Accounts.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithAccountName_ReturnsSpecificAccountBalance()
    {
        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-1" }, { "UserId", TestUserId }, { "Name", "Cash Wallet" }, { "AccountType", "Cash" }, { "Balance", 2500m }, { "Currency", "BDT" }, { "IsClosed", false } },
            new() { { "_id", "acc-2" }, { "UserId", TestUserId }, { "Name", "City Bank Savings" }, { "AccountType", "Bank" }, { "Balance", 80000m }, { "Currency", "BDT" }, { "IsClosed", false } }
        };

        var db = AssistantTestHelpers.MockDatabase(new() { ["accounts"] = accounts });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetPortfolioOrAccountBalanceHandler(db, currentUser);
        var result = await handler.Handle(new GetPortfolioOrAccountBalanceQuery(AccountName: "city bank"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalBalance.Should().Be(80000m);
        result.Value.TargetAccount.Should().NotBeNull();
        result.Value.TargetAccount!.Name.Should().Be("City Bank Savings");
        result.Value.TargetAccount.Balance.Should().Be(80000m);
    }

    [Fact]
    public async Task Handle_WithAccountId_ReturnsSpecificAccountBalance()
    {
        var accounts = new List<BsonDocument>
        {
            new() { { "_id", "acc-1" }, { "UserId", TestUserId }, { "Name", "Cash" }, { "AccountType", "Cash" }, { "Balance", 1500m }, { "Currency", "BDT" }, { "IsClosed", false } }
        };

        var db = AssistantTestHelpers.MockDatabase(new() { ["accounts"] = accounts });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetPortfolioOrAccountBalanceHandler(db, currentUser);
        var result = await handler.Handle(new GetPortfolioOrAccountBalanceQuery(AccountId: "acc-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TargetAccount.Should().NotBeNull();
        result.Value.TargetAccount!.Id.Should().Be("acc-1");
    }
}
