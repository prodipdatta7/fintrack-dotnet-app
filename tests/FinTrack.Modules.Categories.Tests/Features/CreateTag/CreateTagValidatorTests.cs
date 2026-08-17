using FinTrack.Modules.Categories.Features.CreateTag;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Categories.Tests.Features.CreateTag;

public class CreateTagValidatorTests
{
    private readonly CreateTagValidator _validator = new();

    [Fact]
    public void Validate_WhenValidName_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateTagCommand("Tax-Deductible");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyName_ReturnsError()
    {
        // Arrange
        var command = new CreateTagCommand("");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        // Arrange
        var command = new CreateTagCommand(new string('a', 61));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
