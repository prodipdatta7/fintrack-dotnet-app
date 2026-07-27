using FinTrack.Modules.Users.Features.Register;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.Register;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new RegisterCommand("john@example.com", "Password123!", "John", "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password123!", "John", "Doe")]
    [InlineData("invalid-email", "Password123!", "John", "Doe")]
    [InlineData("john@example.com", "short", "John", "Doe")]
    [InlineData("john@example.com", "Password123!", "", "Doe")]
    [InlineData("john@example.com", "Password123!", "John", "")]
    public void Validate_WhenInvalidCommand_ReturnsErrors(
        string email, string password, string firstName, string lastName)
    {
        // Arrange
        var command = new RegisterCommand(email, password, firstName, lastName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
