using FinTrack.Modules.Users.Features.UpdateProfile;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.UpdateProfile;

public class UpdateProfileValidatorTests
{
    private readonly UpdateProfileValidator _validator = new();

    [Fact]
    public void Validate_WhenValidProfileData_ReturnsNoErrors()
    {
        // Arrange
        var command = new UpdateProfileCommand("John", "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Doe")]
    [InlineData("John", "")]
    public void Validate_WhenInvalidProfileData_ReturnsErrors(string firstName, string lastName)
    {
        // Arrange
        var command = new UpdateProfileCommand(firstName, lastName);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}