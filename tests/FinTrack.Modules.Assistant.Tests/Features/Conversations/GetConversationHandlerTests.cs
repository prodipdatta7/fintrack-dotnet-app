using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.GetConversation;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class GetConversationHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_WhenConversationExists_ReturnsDetailWithMessages()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Grocery Planning" }
        };

        var msgs = new List<AssistantMessage>
        {
            new() { Id = "m-1", ConversationId = "c-1", UserId = TestUserId, Content = "Add 300 tk", Role = "user", CreatedAt = DateTime.UtcNow.AddMinutes(-2) },
            new() { Id = "m-2", ConversationId = "c-1", UserId = TestUserId, Content = "Proposed 300 tk expense", Role = "assistant", CreatedAt = DateTime.UtcNow.AddMinutes(-1) }
        };

        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetConversationHandler(db, currentUser);
        var result = await handler.Handle(new GetConversationQuery("c-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("c-1");
        result.Value.Title.Should().Be("Grocery Planning");
        result.Value.Messages.Should().HaveCount(2);
        result.Value.Messages.First().Role.Should().Be("user");
        result.Value.Messages.Last().Role.Should().Be("assistant");
    }

    [Fact]
    public async Task Handle_WhenConversationNotFound_ReturnsFailure()
    {
        var convs = new List<AssistantConversation>();
        var msgs = new List<AssistantMessage>();
        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);

        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetConversationHandler(db, currentUser);
        var result = await handler.Handle(new GetConversationQuery("nonexistent"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }
}
