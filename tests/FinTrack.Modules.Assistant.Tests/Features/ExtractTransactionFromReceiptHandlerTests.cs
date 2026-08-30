using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.ExtractTransactionFromReceipt;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ExtractTransactionFromReceiptHandlerTests
{
    private const string TestUserId = "user-receipt-123";

    [Fact]
    public async Task Handle_ExtractsAmountAndDetailsFromReceiptText()
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

        var handler = new ExtractTransactionFromReceiptHandler(db, currentUser);
        var command = new ExtractTransactionFromReceiptCommand(
            RawText: "Shwapno Supermarket\nDate: 2026-08-15\nTotal: ৳1,450.00\nThank you for shopping!");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTransaction");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Payload.Amount.Should().Be(1450m);
        result.Value.Payload.Title.Should().Be("Shwapno Supermarket");
        result.Value.Payload.CategoryId.Should().Be("cat-food");
        result.Value.Payload.AccountId.Should().Be("acc-cash");
    }
}
