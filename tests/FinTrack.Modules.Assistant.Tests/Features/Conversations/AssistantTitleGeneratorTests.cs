using FinTrack.Modules.Assistant.Services;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Assistant.Tests.Features.Conversations;

public class AssistantTitleGeneratorTests
{
    [Theory]
    [InlineData(null, "New Conversation")]
    [InlineData("", "New Conversation")]
    [InlineData("   ", "New Conversation")]
    [InlineData("add ৳500 for groceries", "Add ৳500 for groceries")]
    [InlineData("what is my current balance in all accounts?", "What is my current balance in all")]
    public void GenerateTitle_ProducesCleanConciseTitle(string? input, string expected)
    {
        var result = AssistantTitleGenerator.GenerateTitle(input);
        result.Should().Be(expected);
    }
}
