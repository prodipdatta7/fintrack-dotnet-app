using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.ProposeCreateCategory;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeCreateCategoryHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_ReturnsProposedActionWithCategoryAttributes()
    {
        var categories = new List<BsonDocument>();
        var db = AssistantTestHelpers.MockDatabase(new() { ["categories"] = categories });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeCreateCategoryHandler(db, currentUser);
        var command = new ProposeCreateCategoryCommand(
            Name: "Gaming & Apps",
            Type: "Expense",
            BudgetLimit: 3000m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddCategory");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Payload.Name.Should().Be("Gaming & Apps");
        result.Value.Payload.BudgetLimit.Should().Be(3000m);
        result.Value.Payload.AlreadyExists.Should().BeFalse();
    }
}
