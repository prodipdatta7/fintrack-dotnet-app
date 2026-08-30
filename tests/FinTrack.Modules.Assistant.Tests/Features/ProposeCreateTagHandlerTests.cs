using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Features.ProposeCreateTag;
using FluentAssertions;
using MongoDB.Bson;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features;

public class ProposeCreateTagHandlerTests
{
    private const string TestUserId = "user-test-123";

    [Fact]
    public async Task Handle_NormalizesTagNameAndReturnsProposedAction()
    {
        var tags = new List<BsonDocument>();
        var db = AssistantTestHelpers.MockDatabase(new() { ["tags"] = tags });

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new ProposeCreateTagHandler(db, currentUser);
        var command = new ProposeCreateTagCommand("#OfficeExpenses");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ActionType.Should().Be("AddTag");
        result.Value.Status.Should().Be("Proposed");
        result.Value.Payload.Name.Should().Be("OfficeExpenses");
        result.Value.Payload.NormalizedName.Should().Be("officeexpenses");
        result.Value.Payload.AlreadyExists.Should().BeFalse();
    }
}
