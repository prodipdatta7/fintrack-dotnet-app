using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Dtos;
using FinTrack.Modules.Assistant.Features.ProposeTransfer;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeTransferHandlerTests
{
    private const string TestUserId = "user-trans-123";

    [Fact]
    public async Task Handle_ProposesTransfer_WithMatchedAccounts()
    {
        var accounts = new List<BsonDocument>
        {
            new()
            {
                ["_id"] = ObjectId.GenerateNewId(),
                ["UserId"] = TestUserId,
                ["Name"] = "City Bank",
                ["Type"] = "Bank",
                ["Balance"] = 50000m,
                ["IsClosed"] = false
            },
            new()
            {
                ["_id"] = ObjectId.GenerateNewId(),
                ["UserId"] = TestUserId,
                ["Name"] = "bKash",
                ["Type"] = "MFS",
                ["Balance"] = 2500m,
                ["IsClosed"] = false
            }
        };

        var mockDb = AssistantTestHelpers.MockDatabase(new Dictionary<string, List<BsonDocument>>
        {
            ["accounts"] = accounts
        });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeTransferHandler(mockDb, currentUser);
        var command = new ProposeTransferCommand(5000m, "City Bank", "bKash", null, "Transfer to wallet");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("TransferFunds");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Summary.Should().Contain("Transfer ৳5,000 from account \"City Bank\" to \"bKash\"");
        result.Value.Payload.Amount.Should().Be(5000m);
        result.Value.Payload.FromAccountName.Should().Be("City Bank");
        result.Value.Payload.ToAccountName.Should().Be("bKash");
    }

    [Fact]
    public async Task Handle_Fails_WhenAmountIsZeroOrNegative()
    {
        var mockDb = AssistantTestHelpers.MockDatabase(new Dictionary<string, List<BsonDocument>>
        {
            ["accounts"] = new List<BsonDocument>()
        });
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeTransferHandler(mockDb, currentUser);
        var command = new ProposeTransferCommand(0m, "City Bank", "bKash");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("greater than zero");
    }
}
