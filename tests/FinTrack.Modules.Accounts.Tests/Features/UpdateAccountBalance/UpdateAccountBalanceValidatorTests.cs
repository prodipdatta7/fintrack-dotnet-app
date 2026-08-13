using FinTrack.Modules.Accounts.Features.UpdateAccountBalance;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Accounts.Tests.Features.UpdateAccountBalance;

public class UpdateAccountBalanceValidatorTests
{
    private readonly UpdateAccountBalanceValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new UpdateAccountBalanceCommand("6899f0a1b2c3d4e5f6a7b8c9", 1250.75m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenZeroBalance_ReturnsNoErrors()
    {
        // Arrange
        var command = new UpdateAccountBalanceCommand("6899f0a1b2c3d4e5f6a7b8c9", 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNegativeBalance_ReturnsError()
    {
        // Arrange
        var command = new UpdateAccountBalanceCommand("6899f0a1b2c3d4e5f6a7b8c9", -0.01m);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenEmptyId_ReturnsError()
    {
        // Arrange
        var command = new UpdateAccountBalanceCommand("", 100);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
