using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.ProposeCreateAccount;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeCreateAccountHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsProposedActionWithAccountDetails()
    {
        var accounts = new List<BsonDocument>();
        var db = AssistantTestHelpers.MockDatabase(new() { ["accounts"] = accounts });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeCreateAccountHandler(db, currentUser);
        var command = new ProposeCreateAccountCommand(
            Name: "Standard Chartered",
            Type: "Bank",
            InitialBalance: 15000m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddAccount");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Payload.Name.Should().Be("Standard Chartered");
        result.Value.Payload.AccountType.Should().Be("Bank");
        result.Value.Payload.InitialBalance.Should().Be(15000m);
        result.Value.Payload.AlreadyExists.Should().BeFalse();
    }
}
