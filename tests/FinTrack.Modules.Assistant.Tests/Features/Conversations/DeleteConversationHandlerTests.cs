using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.DeleteConversation;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class DeleteConversationHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_DeletesConversationAndItsMessages()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "To Delete" }
        };

        var msgs = new List<AssistantMessage>
        {
            new() { Id = "m-1", ConversationId = "c-1", UserId = TestUserId, Content = "Msg 1" },
            new() { Id = "m-2", ConversationId = "c-1", UserId = TestUserId, Content = "Msg 2" }
        };

        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new DeleteConversationHandler(db, currentUser);
        var result = await handler.Handle(new DeleteConversationCommand("c-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        convs.Should().BeEmpty();
        msgs.Should().BeEmpty();
    }
}
