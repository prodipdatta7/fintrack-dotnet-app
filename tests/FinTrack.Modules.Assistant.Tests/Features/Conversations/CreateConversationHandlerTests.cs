using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.CreateConversation;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class CreateConversationHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_WithInitialMessage_CreatesConversationAndFirstMessage()
    {
        var conversations = new List<AssistantConversation>();
        var messages = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(conversations, messages);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new CreateConversationHandler(db, currentUser);
        var command = new CreateConversationCommand(InitialMessage: "Show my top spending categories");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Show my top spending categories");
        result.Value.MessageCount.Should().Be(1);
        conversations.Should().HaveCount(1);
        messages.Should().HaveCount(1);
        messages.First().Content.Should().Be("Show my top spending categories");
        messages.First().Role.Should().Be("user");
    }

    [Fact]
    public async Task Handle_WithExplicitTitle_UsesExplicitTitle()
    {
        var conversations = new List<AssistantConversation>();
        var messages = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(conversations, messages);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new CreateConversationHandler(db, currentUser);
        var command = new CreateConversationCommand(Title: "August Financial Review");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("August Financial Review");
        result.Value.MessageCount.Should().Be(0);
        conversations.Should().HaveCount(1);
    }
}
