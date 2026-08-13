using FinTrack.Modules.Budgets.Features.UpdatePlan;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Budgets.Tests.Features.UpdatePlan;

public class UpdatePlanValidatorTests
{
    private readonly UpdatePlanValidator _validator = new();

    private static UpdatePlanCommand ValidCommand(
        string id = "665f1f77bcf86cd799439011",
        string title = "Emergency Fund",
        decimal targetAmount = 5000,
        decimal currentAmount = 250,
        string color = "#3498db",
        DateTime? deadline = null) =>
        new(id, title, targetAmount, currentAmount, color, deadline ?? DateTime.UtcNow.AddMonths(6));

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
    public void Validate_WhenDeadlineInPast_ReturnsNoErrors()
    {
        // A past deadline is legal on update — the user may edit a plan whose
        // deadline has already elapsed (unlike CreatePlanValidator).

        // Arrange
        var command = ValidCommand(deadline: DateTime.UtcNow.AddDays(-1));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenDefaultDeadline_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(deadline: default(DateTime));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#12345G")]
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
