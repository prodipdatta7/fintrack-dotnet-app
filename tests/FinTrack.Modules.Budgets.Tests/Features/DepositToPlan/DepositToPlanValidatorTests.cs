using FinTrack.Modules.Budgets.Features.DepositToPlan;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Budgets.Tests.Features.DepositToPlan;

public class DepositToPlanValidatorTests
{
    private readonly DepositToPlanValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new DepositToPlanCommand("665f1f77bcf86cd799439011", 50);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyId_ReturnsError()
    {
        // Arrange
        var command = new DepositToPlanCommand("", 50);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public void Validate_WhenAmountNotPositive_ReturnsError(decimal amount)
    {
        // Arrange
        var command = new DepositToPlanCommand("665f1f77bcf86cd799439011", amount);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
