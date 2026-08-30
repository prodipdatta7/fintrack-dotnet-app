using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.UpdateConversationTitle;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class UpdateConversationTitleHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_UpdatesTitleSuccessfully()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Old Title" }
        };
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new UpdateConversationTitleHandler(db, currentUser);
        var result = await handler.Handle(new UpdateConversationTitleCommand("c-1", "Renamed Title"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Renamed Title");
    }
}
