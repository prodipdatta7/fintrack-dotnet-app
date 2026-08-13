using FinTrack.Modules.Dashboard.Features.GetCashflowSeries;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Dashboard.Tests.Features.GetCashflowSeries;

public class CashflowBucketsTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 12, 14, 30, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("7D", 7)]
    [InlineData("15D", 15)]
    [InlineData("30D", 30)]
    [InlineData("60D", 60)]
    public void Build_DayTimeframes_ProduceOneBucketPerDayEndingToday(string timeframe, int expectedDays)
    {
        // Act
        var plan = CashflowBuckets.Build(timeframe, null, null, UtcNow);

        // Assert
        plan.UseMonthBuckets.Should().BeFalse();
        plan.Buckets.Should().HaveCount(expectedDays);
        plan.StartUtc.Should().Be(new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc).AddDays(-(expectedDays - 1)));
        plan.EndExclusiveUtc.Should().Be(new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc));
        plan.Buckets[^1].Key.Should().Be("2026-08-12");
        plan.Buckets[^1].Label.Should().Be("Aug 12");
    }

    [Fact]
    public void Build_7D_ProducesExpectedKeysAndLabels()
    {
        // Act
        var plan = CashflowBuckets.Build("7D", null, null, UtcNow);

        // Assert
        plan.Buckets.Select(b => b.Key).Should().Equal(
            "2026-08-06", "2026-08-07", "2026-08-08", "2026-08-09",
            "2026-08-10", "2026-08-11", "2026-08-12");
        plan.Buckets.Select(b => b.Label).Should().Equal(
            "Aug 6", "Aug 7", "Aug 8", "Aug 9", "Aug 10", "Aug 11", "Aug 12");
    }

    [Fact]
    public void Build_6M_ProducesSixMonthBucketsEndingCurrentMonth()
    {
        // Act
        var plan = CashflowBuckets.Build("6M", null, null, UtcNow);

        // Assert
        plan.UseMonthBuckets.Should().BeTrue();
        plan.StartUtc.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        plan.EndExclusiveUtc.Should().Be(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        plan.Buckets.Select(b => b.Key).Should().Equal(
            "2026-03", "2026-04", "2026-05", "2026-06", "2026-07", "2026-08");
        plan.Buckets.Select(b => b.Label).Should().Equal(
            "Mar", "Apr", "May", "Jun", "Jul", "Aug");
    }

    [Fact]
    public void Build_1Y_ProducesTwelveMonthBucketsCrossingYearBoundary()
    {
        // Act
        var plan = CashflowBuckets.Build("1Y", null, null, UtcNow);

        // Assert
        plan.UseMonthBuckets.Should().BeTrue();
        plan.Buckets.Should().HaveCount(12);
        plan.StartUtc.Should().Be(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        plan.Buckets[0].Key.Should().Be("2025-09");
        plan.Buckets[0].Label.Should().Be("Sep");
        plan.Buckets[^1].Key.Should().Be("2026-08");
        plan.Buckets[^1].Label.Should().Be("Aug");
    }

    [Fact]
    public void Build_DayRangeCrossingYearBoundary_KeysCarryTheYear()
    {
        // Arrange
        var utcNow = new DateTime(2026, 1, 2, 8, 0, 0, DateTimeKind.Utc);

        // Act
        var plan = CashflowBuckets.Build("7D", null, null, utcNow);

        // Assert
        plan.Buckets.Select(b => b.Key).Should().Equal(
            "2025-12-27", "2025-12-28", "2025-12-29", "2025-12-30",
            "2025-12-31", "2026-01-01", "2026-01-02");
        plan.Buckets[0].Label.Should().Be("Dec 27");
        plan.Buckets[^1].Label.Should().Be("Jan 2");
    }

    [Fact]
    public void Build_Custom_ProducesInclusiveDayBucketsFromRange()
    {
        // Arrange
        var from = new DateTime(2026, 1, 30);
        var to = new DateTime(2026, 2, 2);

        // Act
        var plan = CashflowBuckets.Build("Custom", from, to, UtcNow);

        // Assert
        plan.UseMonthBuckets.Should().BeFalse();
        plan.StartUtc.Should().Be(new DateTime(2026, 1, 30, 0, 0, 0, DateTimeKind.Utc));
        plan.EndExclusiveUtc.Should().Be(new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc));
        plan.Buckets.Select(b => b.Key).Should().Equal(
            "2026-01-30", "2026-01-31", "2026-02-01", "2026-02-02");
        plan.Buckets.Select(b => b.Label).Should().Equal(
            "Jan 30", "Jan 31", "Feb 1", "Feb 2");
    }

    [Fact]
    public void Build_CustomSingleDay_ProducesOneBucket()
    {
        // Arrange
        var day = new DateTime(2026, 8, 12);

        // Act
        var plan = CashflowBuckets.Build("Custom", day, day, UtcNow);

        // Assert
        plan.Buckets.Should().ContainSingle()
            .Which.Should().Be(new CashflowBuckets.Bucket("2026-08-12", "Aug 12"));
    }

    [Fact]
    public void Build_CustomIgnoresTimeOfDayOnBoundaries()
    {
        // Arrange
        var from = new DateTime(2026, 8, 10, 23, 59, 0);
        var to = new DateTime(2026, 8, 11, 0, 1, 0);

        // Act
        var plan = CashflowBuckets.Build("Custom", from, to, UtcNow);

        // Assert
        plan.Buckets.Should().HaveCount(2);
        plan.StartUtc.Should().Be(new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc));
        plan.EndExclusiveUtc.Should().Be(new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("7d")]
    [InlineData("6m")]
    [InlineData("custom")]
    public void Build_TimeframeIsCaseInsensitive(string timeframe)
    {
        // Arrange
        var from = new DateTime(2026, 8, 1);
        var to = new DateTime(2026, 8, 12);

        // Act
        var act = () => CashflowBuckets.Build(timeframe, from, to, UtcNow);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_UnknownTimeframe_Throws()
    {
        // Act
        var act = () => CashflowBuckets.Build("90D", null, null, UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllowedTimeframes_MatchTheContract()
    {
        // Assert — REQ-013
        CashflowBuckets.AllowedTimeframes.Should().Equal(
            "7D", "15D", "30D", "60D", "6M", "1Y", "Custom");
    }
}
