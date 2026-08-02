using FinTrack.Modules.Users.Features.UpdateSettings;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Users.Tests.Features.UpdateSettings;

public class UpdateSettingsValidatorTests
{
    private readonly UpdateSettingsValidator _validator = new();

    [Fact]
    public void Validate_WhenValidSettings_ReturnsNoErrors()
    {
        // Arrange
        var command = new UpdateSettingsCommand("BDT", "Asia/Dhaka", "dd/MM/yyyy", 10, true, true, 5000);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Asia/Dhaka", "dd/MM/yyyy", 10)]
    [InlineData("BDT", "", "dd/MM/yyyy", 10)]
    [InlineData("BDT", "Asia/Dhaka", "", 10)]
    [InlineData("BDT", "Asia/Dhaka", "dd/MM/yyyy", 2)] // Page size too small
    [InlineData("BDT", "Asia/Dhaka", "dd/MM/yyyy", 500)] // Page size too large
    public void Validate_WhenInvalidSettings_ReturnsErrors(string currency, string timeZone, string dateFormat, int pageSize)
    {
        // Arrange
        var command = new UpdateSettingsCommand(currency, timeZone, dateFormat, pageSize, true, true, null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
