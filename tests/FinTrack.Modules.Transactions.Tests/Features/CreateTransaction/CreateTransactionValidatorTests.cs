using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.CreateTransaction;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.CreateTransaction;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Title = "Grocery Shopping",
            Amount = 45.50m,
            Type = TransactionType.Expense,
            CategoryId = "cat123",
            AccountId = "acc123",
            Date = DateTime.UtcNow,
            TimeZoneOffsetInMinutes = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 45.50, "cat123", "acc123")]
    [InlineData("Grocery", 0, "cat123", "acc123")]
    [InlineData("Grocery", -10, "cat123", "acc123")]
    [InlineData("Grocery", 45.50, "", "acc123")]
    [InlineData("Grocery", 45.50, "cat123", "")]
    public void Validate_WhenInvalidCommand_ReturnsErrors(
        string title, decimal amount, string categoryId, string accountId)
    {
        // Arrange
        var command = new CreateTransactionCommand
        {
            Title = title,
            Amount = amount,
            Type = TransactionType.Expense,
            CategoryId = categoryId,
            AccountId = accountId,
            Date = DateTime.UtcNow,
            TimeZoneOffsetInMinutes = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
