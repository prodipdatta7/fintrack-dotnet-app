using FinTrack.Modules.Categories.Domain;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Categories.Tests.Domain;

public class NameKeysTests
{
    [Theory]
    [InlineData("Travel", "travel")]
    [InlineData("  Travel  ", "travel")]
    [InlineData("#Travel", "travel")]
    [InlineData("  #Travel  ", "travel")]
    [InlineData("Food & Dining", "food & dining")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Normalize_WhenInputVariesByCaseAndFormat_ReturnsLowerCaseKey(string input, string expected)
    {
        NameKeys.Normalize(input).Should().Be(expected);
    }
}