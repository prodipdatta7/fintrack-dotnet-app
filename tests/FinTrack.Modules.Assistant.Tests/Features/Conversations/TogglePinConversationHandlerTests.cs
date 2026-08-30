using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.TogglePinConversation;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class TogglePinConversationHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_TogglesPinSuccessfully()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "My Chat", IsPinned = false }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new TogglePinConversationHandler(db, currentUser);
        var result = await handler.Handle(new TogglePinConversationCommand("c-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenConversationNotFound()
    {
        var convs = new List<AssistantConversation>();
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new TogglePinConversationHandler(db, currentUser);
        var result = await handler.Handle(new TogglePinConversationCommand("c-999"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
