using FinTrack.Modules.Dashboard.Features.GetCashflowSeries;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Dashboard.Tests.Features.GetCashflowSeries;

public class GetCashflowSeriesValidatorTests
{
    private readonly GetCashflowSeriesValidator _validator = new();

    [Theory]
    [InlineData("7D")]
    [InlineData("15D")]
    [InlineData("30D")]
    [InlineData("60D")]
    [InlineData("6M")]
    [InlineData("1Y")]
    public void Validate_WhenAllowedFixedTimeframe_ReturnsNoErrors(string timeframe)
    {
        // Arrange
        var query = new GetCashflowSeriesQuery(timeframe);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("7d")]
    [InlineData("6m")]
    [InlineData("1y")]
    public void Validate_WhenTimeframeCaseDiffers_ReturnsNoErrors(string timeframe)
    {
        // Arrange
        var query = new GetCashflowSeriesQuery(timeframe);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("90D")]
    [InlineData("weekly")]
    [InlineData("3M")]
    public void Validate_WhenUnknownTimeframe_ReturnsError(string timeframe)
    {
        // Arrange
        var query = new GetCashflowSeriesQuery(timeframe);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCustomWithValidRange_ReturnsNoErrors()
    {
        // Arrange
        var query = new GetCashflowSeriesQuery(
            "Custom", new DateTime(2026, 1, 1), new DateTime(2026, 3, 31));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenCustomMissingFrom_ReturnsError()
    {
        // Arrange
        var query = new GetCashflowSeriesQuery("Custom", From: null, To: new DateTime(2026, 3, 31));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCustomMissingTo_ReturnsError()
    {
        // Arrange
        var query = new GetCashflowSeriesQuery("Custom", From: new DateTime(2026, 1, 1), To: null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCustomMissingBoth_ReturnsError()
    {
        // Arrange
        var query = new GetCashflowSeriesQuery("Custom");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCustomFromAfterTo_ReturnsError()
    {
        // Arrange
        var query = new GetCashflowSeriesQuery(
            "Custom", new DateTime(2026, 3, 31), new DateTime(2026, 1, 1));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCustomFromEqualsTo_ReturnsNoErrors()
    {
        // Arrange
        var day = new DateTime(2026, 8, 12);
        var query = new GetCashflowSeriesQuery("Custom", day, day);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenCustomRangeExactly366Days_ReturnsNoErrors()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var query = new GetCashflowSeriesQuery("Custom", from, from.AddDays(366));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenCustomRangeExceeds366Days_ReturnsError()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var query = new GetCashflowSeriesQuery("Custom", from, from.AddDays(367));

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenFixedTimeframeWithoutDates_ReturnsNoErrors()
    {
        // Arrange — from/to are only required for Custom.
        var query = new GetCashflowSeriesQuery("7D", From: null, To: null);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
