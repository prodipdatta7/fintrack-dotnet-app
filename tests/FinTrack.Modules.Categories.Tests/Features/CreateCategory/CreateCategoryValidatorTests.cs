using FinTrack.Modules.Categories.Domain;
using FinTrack.Modules.Categories.Features.CreateCategory;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Categories.Tests.Features.CreateCategory;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateCategoryCommand("Freelance", CategoryType.Income, "briefcase", "#16a085");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyName_ReturnsError()
    {
        // Arrange
        var command = new CreateCategoryCommand("", CategoryType.Income, "briefcase", "#16a085");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenBudgetLimitIsZero_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateCategoryCommand("Groceries", CategoryType.Expense, "cart", "#e74c3c", 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenBudgetLimitIsNegative_ReturnsError()
    {
        // Arrange
        var command = new CreateCategoryCommand("Groceries", CategoryType.Expense, "cart", "#e74c3c", -1);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
