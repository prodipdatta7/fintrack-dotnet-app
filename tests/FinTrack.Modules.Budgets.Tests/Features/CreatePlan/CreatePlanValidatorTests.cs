using FinTrack.Modules.Budgets.Features.CreatePlan;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Budgets.Tests.Features.CreatePlan;

public class CreatePlanValidatorTests
{
    private readonly CreatePlanValidator _validator = new();

    private static CreatePlanCommand ValidCommand(
        string title = "Emergency Fund",
        decimal targetAmount = 5000,
        decimal currentAmount = 250,
        string color = "#3498db",
        DateTime? deadline = null) =>
        new(title, targetAmount, currentAmount, color, deadline ?? DateTime.UtcNow.AddMonths(6));

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
    public void Validate_WhenEmptyTitle_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(title: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenTitleTooLong_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(title: new string('a', 101));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenTitleAtMaxLength_ReturnsNoErrors()
    {
        // Arrange
        var command = ValidCommand(title: new string('a', 100));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenTargetAmountNotPositive_ReturnsError(decimal targetAmount)
    {
        // Arrange
        var command = ValidCommand(targetAmount: targetAmount);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenNegativeCurrentAmount_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(currentAmount: -1);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenZeroCurrentAmount_ReturnsNoErrors()
    {
        // Arrange
        var command = ValidCommand(currentAmount: 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDeadlineInPast_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(deadline: DateTime.UtcNow.AddDays(-1));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#12345G")]
    [InlineData("")]
    public void Validate_WhenInvalidColor_ReturnsError(string color)
    {
        // Arrange
        var command = ValidCommand(color: color);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
