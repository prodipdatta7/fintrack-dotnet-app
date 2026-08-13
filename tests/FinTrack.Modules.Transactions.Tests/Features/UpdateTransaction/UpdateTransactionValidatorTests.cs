using FinTrack.Modules.Transactions.Domain;
using FinTrack.Modules.Transactions.Features.UpdateTransaction;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Transactions.Tests.Features.UpdateTransaction;

public class UpdateTransactionValidatorTests
{
    private readonly UpdateTransactionValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ReturnsNoErrors()
    {
        // Arrange
        var command = ValidCommandWithNote(new string('x', 500));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNoteExceedsMaxLength_ReturnsError()
    {
        // Arrange
        var command = ValidCommandWithNote(new string('x', 501));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Note");
    }

    private static UpdateTransactionCommand ValidCommandWithNote(string note) => new()
    {
        Id = "tx-1",
        Title = "Grocery Shopping",
        Amount = 45.50m,
        Type = TransactionType.Expense,
        CategoryId = "cat123",
        AccountId = "acc123",
        Date = DateTime.UtcNow,
        Note = note
    };
}
