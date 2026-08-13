using FinTrack.Modules.Accounts.Features.UpdateAccount;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Accounts.Tests.Features.UpdateAccount;

public class UpdateAccountValidatorTests
{
    private readonly UpdateAccountValidator _validator = new();

    private static UpdateAccountCommand ValidCommand(
        string id = "6899f0a1b2c3d4e5f6a7b8c9",
        string name = "bKash Wallet",
        string accountType = "MFS",
        string color = "#e2136e") =>
        new(id, name, accountType, "BDT", "📱", "bKash", color);

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyId_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(id: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenEmptyName_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(name: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenUnknownAccountType_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(accountType: "Wallet");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenInvalidColor_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(color: "#12");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
