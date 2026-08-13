using FinTrack.Modules.Users.Features.ChangePassword;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Validate_WhenPasswordMeetsAllCriteria_ReturnsValidResult()
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPass123!", "NewStrongP@ss1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1!", "Password must be at least 8 characters.")]
    [InlineData("alllowercase123!", "Password must contain at least one uppercase letter.")]
    [InlineData("ALLUPPERCASE123!", "Password must contain at least one lowercase letter.")]
    [InlineData("NoDigitsHere!", "Password must contain at least one digit.")]
    [InlineData("NoSpecialChars123", "Password must contain at least one special character.")]
    public void Validate_WhenPasswordFailsRule_ReturnsValidationError(string invalidNewPassword, string expectedErrorSubstring)
    {
        // Arrange
        var command = new ChangePasswordCommand("OldPass123!", invalidNewPassword);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedErrorSubstring));
    }
}
