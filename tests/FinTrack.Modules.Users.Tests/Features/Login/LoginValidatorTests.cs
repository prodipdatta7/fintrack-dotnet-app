using FinTrack.Modules.Users.Features.Login;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCredentials_ReturnsNoErrors()
    {
        // Arrange
        var command = new LoginCommand("user@example.com", "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password123!")]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("user@example.com", "")]
    public void Validate_WhenInvalidCredentials_ReturnsErrors(string email, string password)
    {
        // Arrange
        var command = new LoginCommand(email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
