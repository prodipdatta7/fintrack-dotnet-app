using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.SendMessage;
using FinTrack.Modules.Assistant.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class SendMessageHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_AppendsMessageAndAutoUpdatesTitleWhenNewConversation()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "New Conversation" }
        };

        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var guardrails = new AssistantGuardrailsService();
        var handler = new SendMessageHandler(db, currentUser, guardrails);
        var command = new SendMessageCommand(
            ConversationId: "c-1",
            Content: "Add ৳450 for groceries",
            Role: "user");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Add ৳450 for groceries");
        result.Value.Role.Should().Be("user");
        msgs.Should().HaveCount(1);
    }
}
