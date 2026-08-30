using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Assistant.Domain;
using FinTrack.Modules.Assistant.Features.Conversations.GetConversations;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class GetConversationsHandlerTests
{
    private const string TestUserId = "user-conv-123";

    [Fact]
    public async Task Handle_ReturnsUserConversationsList()
    {
        var convs = new List<AssistantConversation>
        {
            new() { Id = "c-1", UserId = TestUserId, Title = "Grocery Planning", LastMessageAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = "c-2", UserId = TestUserId, Title = "Savings Goals", LastMessageAt = DateTime.UtcNow.AddHours(-1) }
        };

        var msgs = new List<AssistantMessage>
        {
            new() { Id = "m-1", ConversationId = "c-1", UserId = TestUserId, Content = "Add expense", Role = "user" }
        };

        var db = AssistantTestHelpers.MockConversationDatabase(convs, msgs);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestUserId);

        var handler = new GetConversationsHandler(db, currentUser);
        var result = await handler.Handle(new GetConversationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.First(c => c.Id == "c-1").MessageCount.Should().Be(1);
    }
}
