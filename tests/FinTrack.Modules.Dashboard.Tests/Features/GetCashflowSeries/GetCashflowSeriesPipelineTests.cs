using FinTrack.Modules.Dashboard.Features.GetCashflowSeries;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace FinTrack.Modules.Dashboard.Tests.Features.GetCashflowSeries;

public class GetCashflowSeriesPipelineTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 12, 14, 30, 0, DateTimeKind.Utc);

    private static CashflowBuckets.BucketPlan Plan(string timeframe = "7D") =>
        CashflowBuckets.Build(timeframe, null, null, UtcNow);

    [Fact]
    public void BuildStages_ScopesMatchToUserAndUtcWindow()
    {
        // Arrange
        var plan = Plan();

        // Act
        var stages = GetCashflowSeriesPipeline.BuildStages("user-1", plan, accountId: null);

        // Assert
        stages.Should().HaveCount(2);
        var match = stages[0]["$match"].AsBsonDocument;
        match["UserId"].AsString.Should().Be("user-1");
        match["date"]["$gte"].ToUniversalTime().Should().Be(plan.StartUtc);
        match["date"]["$lt"].ToUniversalTime().Should().Be(plan.EndExclusiveUtc);
        match.Contains("accountId").Should().BeFalse();
    }

    [Fact]
    public void BuildStages_WhenAccountIdProvided_AddsItToTheMatch()
    {
        // Act
        var stages = GetCashflowSeriesPipeline.BuildStages("user-1", Plan(), "acc-9");

        // Assert
        stages[0]["$match"]["accountId"].AsString.Should().Be("acc-9");
    }

    [Fact]
    public void BuildStages_DayPlan_GroupsByDayKeyFormat()
    {
        // Act
        var stages = GetCashflowSeriesPipeline.BuildStages("user-1", Plan("30D"), null);

        // Assert
        var group = stages[1]["$group"].AsBsonDocument;
        group["_id"]["$dateToString"]["format"].AsString.Should().Be("%Y-%m-%d");
        group["_id"]["$dateToString"]["date"].AsString.Should().Be("$date");
    }

    [Fact]
    public void BuildStages_MonthPlan_GroupsByMonthKeyFormat()
    {
        // Act
        var stages = GetCashflowSeriesPipeline.BuildStages("user-1", Plan("6M"), null);

        // Assert
        stages[1]["$group"]["_id"]["$dateToString"]["format"].AsString.Should().Be("%Y-%m");
    }

    [Fact]
    public void BuildStages_SumsIncomeAndExpenseByTransactionType()
    {
        // Act
        var stages = GetCashflowSeriesPipeline.BuildStages("user-1", Plan(), null);

        // Assert — 1 = Income, 2 = Expense (TransactionType enum values on the wire).
        var group = stages[1]["$group"].AsBsonDocument;
        group["income"]["$sum"]["$cond"][0]["$eq"].AsBsonArray[1].AsInt32.Should().Be(1);
        group["expense"]["$sum"]["$cond"][0]["$eq"].AsBsonArray[1].AsInt32.Should().Be(2);
    }

    [Fact]
    public void MapSeries_ZeroFillsBucketsWithoutData()
    {
        // Arrange — only two of the seven days have transactions (guards REQ-015).
        var plan = Plan();
        var groups = new[]
        {
            new BsonDocument
            {
                { "_id", "2026-08-07" },
                { "income", new BsonDecimal128(120.50m) },
                { "expense", new BsonDecimal128(30m) }
            },
            new BsonDocument
            {
                { "_id", "2026-08-12" },
                { "income", 0 },
                { "expense", new BsonDecimal128(45.25m) }
            }
        };

        // Act
        var series = GetCashflowSeriesPipeline.MapSeries(plan, groups);

        // Assert
        series.Should().HaveCount(7);
        series.Select(p => p.Label).Should().Equal(
            "Aug 6", "Aug 7", "Aug 8", "Aug 9", "Aug 10", "Aug 11", "Aug 12");
        series[1].Should().Be(new CashflowPointDto("Aug 7", 120.50m, 30m));
        series[6].Should().Be(new CashflowPointDto("Aug 12", 0m, 45.25m));
        series.Where((_, i) => i is not 1 and not 6)
            .Should().OnlyContain(p => p.Income == 0m && p.Expense == 0m);
    }

    [Fact]
    public void MapSeries_WhenNoGroups_ReturnsDenseZeroSeries()
    {
        // Arrange
        var plan = Plan("15D");

        // Act
        var series = GetCashflowSeriesPipeline.MapSeries(plan, Array.Empty<BsonDocument>());

        // Assert
        series.Should().HaveCount(15);
        series.Should().OnlyContain(p => p.Income == 0m && p.Expense == 0m);
    }

    [Fact]
    public void MapSeries_MonthPlan_MapsGroupsOntoMonthLabels()
    {
        // Arrange
        var plan = Plan("6M");
        var groups = new[]
        {
            new BsonDocument
            {
                { "_id", "2026-05" },
                { "income", new BsonDecimal128(1000m) },
                { "expense", new BsonDecimal128(400m) }
            }
        };

        // Act
        var series = GetCashflowSeriesPipeline.MapSeries(plan, groups);

        // Assert
        series.Should().HaveCount(6);
        series.Single(p => p.Label == "May").Should().Be(new CashflowPointDto("May", 1000m, 400m));
        series.Where(p => p.Label != "May").Should().OnlyContain(p => p.Income == 0m && p.Expense == 0m);
    }

    [Fact]
    public void MapSeries_IgnoresGroupsOutsideThePlan()
    {
        // Arrange — a stray key (e.g. clock skew) must not throw or leak into the series.
        var plan = Plan();
        var groups = new[]
        {
            new BsonDocument
            {
                { "_id", "2020-01-01" },
                { "income", new BsonDecimal128(999m) },
                { "expense", 0 }
            }
        };

        // Act
        var series = GetCashflowSeriesPipeline.MapSeries(plan, groups);

        // Assert
        series.Should().OnlyContain(p => p.Income == 0m && p.Expense == 0m);
    }
}
