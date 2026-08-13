using FinTrack.Modules.Accounts.Features.CreateAccount;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Accounts.Tests.Features.CreateAccount;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _validator = new();

    private static CreateAccountCommand ValidCommand(
        string name = "City Bank",
        string accountType = "Bank",
        decimal balance = 500,
        string currency = "USD",
        string color = "#3498db") =>
        new(name, accountType, balance, currency, "🏦", "City Bank", color);

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
    public void Validate_WhenNameTooLong_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(name: new string('a', 61));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Wallet")]
    [InlineData("bank")]
    [InlineData("")]
    public void Validate_WhenUnknownAccountType_ReturnsError(string accountType)
    {
        // Arrange
        var command = ValidCommand(accountType: accountType);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("Bank")]
    [InlineData("MFS")]
    [InlineData("Cash")]
    [InlineData("Credit")]
    public void Validate_WhenAllowedAccountType_ReturnsNoErrors(string accountType)
    {
        // Arrange
        var command = ValidCommand(accountType: accountType);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNegativeBalance_ReturnsError()
    {
        // Arrange
        var command = ValidCommand(balance: -1);

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

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDT")]
    public void Validate_WhenInvalidCurrency_ReturnsError(string currency)
    {
        // Arrange
        var command = ValidCommand(currency: currency);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
