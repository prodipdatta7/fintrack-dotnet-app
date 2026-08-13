using FinTrack.Modules.Dashboard.Features.GetDashboardSummary;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace FinTrack.Modules.Dashboard.Tests.Features.GetDashboardSummary;

public class GetDashboardSummaryPipelineTests
{
    [Fact]
    public void BuildStages_ScopesMatchToUserOnlyByDefault()
    {
        // Act
        var stages = GetDashboardSummaryPipeline.BuildStages("user-1", null, null, null);

        // Assert
        stages.Should().HaveCount(2);
        var match = stages[0]["$match"].AsBsonDocument;
        match["UserId"].AsString.Should().Be("user-1");
        match.Contains("date").Should().BeFalse();
        match.Contains("accountId").Should().BeFalse();
    }

    [Fact]
    public void BuildStages_AppliesDateRangeAndAccountFilter()
    {
        // Arrange
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var stages = GetDashboardSummaryPipeline.BuildStages("user-1", from, to, "acc-9");

        // Assert
        var match = stages[0]["$match"].AsBsonDocument;
        match["date"]["$gte"].ToUniversalTime().Should().Be(from);
        match["date"]["$lte"].ToUniversalTime().Should().Be(to);
        match["accountId"].AsString.Should().Be("acc-9");
    }

    [Fact]
    public void BuildStages_SupportsOpenEndedRanges()
    {
        // Act
        var stages = GetDashboardSummaryPipeline.BuildStages(
            "user-1", new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), null, null);

        // Assert
        var range = stages[0]["$match"]["date"].AsBsonDocument;
        range.Contains("$gte").Should().BeTrue();
        range.Contains("$lte").Should().BeFalse();
    }

    [Fact]
    public void BuildStages_FacetCapsRecentTransactionsAtFiveNewestFirst()
    {
        // Act
        var stages = GetDashboardSummaryPipeline.BuildStages("user-1", null, null, null);

        // Assert — REQ-012: the 5 most recent by date desc.
        var recent = stages[1]["$facet"]["recent"].AsBsonArray;
        recent[0]["$sort"]["date"].AsInt32.Should().Be(-1);
        recent[1]["$limit"].AsInt32.Should().Be(5);
    }

    [Fact]
    public void BuildStages_CategorySpentOnlyAggregatesExpenses()
    {
        // Act
        var stages = GetDashboardSummaryPipeline.BuildStages("user-1", null, null, null);

        // Assert — 2 = Expense.
        var categorySpent = stages[1]["$facet"]["categorySpent"].AsBsonArray;
        categorySpent[0]["$match"]["type"].AsInt32.Should().Be(2);
        categorySpent[1]["$group"]["_id"].AsString.Should().Be("$categoryId");
        categorySpent[1]["$group"]["spent"]["$sum"].AsString.Should().Be("$amount");
    }

    [Fact]
    public void Map_ComputesTotalsAndNetSavings()
    {
        // Arrange
        var facetResult = FacetResult(
            totals:
            [
                new BsonDocument { { "_id", 1 }, { "total", new BsonDecimal128(1500m) } },
                new BsonDocument { { "_id", 2 }, { "total", new BsonDecimal128(600.25m) } }
            ],
            count: 42);

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        dto.TotalIncome.Should().Be(1500m);
        dto.TotalExpense.Should().Be(600.25m);
        dto.NetSavings.Should().Be(899.75m);
        dto.TransactionCount.Should().Be(42);
    }

    [Fact]
    public void Map_MapsCategorySpent()
    {
        // Arrange
        var facetResult = FacetResult(categorySpent:
        [
            new BsonDocument { { "_id", "cat-food" }, { "spent", new BsonDecimal128(320m) } },
            new BsonDocument { { "_id", "cat-rent" }, { "spent", new BsonDecimal128(150.50m) } }
        ]);

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        dto.CategorySpent.Should().Equal(
            new CategorySpentDto("cat-food", 320m),
            new CategorySpentDto("cat-rent", 150.50m));
    }

    [Fact]
    public void Map_MapsRecentTransactionsToTheTransactionsApiShape()
    {
        // Arrange
        var objectId = ObjectId.GenerateNewId();
        var date = new DateTime(2026, 8, 10, 9, 15, 0, DateTimeKind.Utc);
        var facetResult = FacetResult(recent:
        [
            new BsonDocument
            {
                { "_id", objectId },
                { "title", "Groceries" },
                { "amount", new BsonDecimal128(84.99m) },
                { "type", 2 },
                { "categoryId", "cat-food" },
                { "accountId", "acc-9" },
                { "date", date },
                { "timeZoneOffsetInMinutes", 360 },
                { "time", "09:15" },
                { "paymentMethod", "Card" },
                { "receiptFileName", "receipt.png" },
                { "receiptUrl", "/files/receipt.png" },
                { "tags", "weekly" },
                {
                    "attachments", new BsonArray
                    {
                        new BsonDocument { { "fileName", "receipt.png" }, { "fileUrl", "/files/receipt.png" } }
                    }
                }
            }
        ]);

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        var tx = dto.RecentTransactions.Should().ContainSingle().Subject;
        tx.Id.Should().Be(objectId.ToString());
        tx.Title.Should().Be("Groceries");
        tx.Amount.Should().Be(84.99m);
        tx.Type.Should().Be(2);
        tx.CategoryId.Should().Be("cat-food");
        tx.AccountId.Should().Be("acc-9");
        tx.Date.Should().Be(date);
        tx.TimeZoneOffsetInMinutes.Should().Be(360);
        tx.Time.Should().Be("09:15");
        tx.PaymentMethod.Should().Be("Card");
        tx.ReceiptFileName.Should().Be("receipt.png");
        tx.ReceiptUrl.Should().Be("/files/receipt.png");
        tx.Tags.Should().Be("weekly");
        tx.Attachments.Should().ContainSingle()
            .Which.Should().Be(new DashboardAttachmentDto("receipt.png", "/files/receipt.png"));
    }

    [Fact]
    public void Map_ToleratesDocumentsWithMissingOptionalFields()
    {
        // Arrange — older documents may lack the optional fields.
        var facetResult = FacetResult(recent:
        [
            new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "title", "Legacy" },
                { "amount", new BsonDecimal128(10m) },
                { "type", 1 },
                { "categoryId", "cat-1" },
                { "accountId", "" },
                { "date", DateTime.UtcNow }
            }
        ]);

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        var tx = dto.RecentTransactions.Should().ContainSingle().Subject;
        tx.Time.Should().BeEmpty();
        tx.PaymentMethod.Should().BeEmpty();
        tx.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void Map_WhenLedgerIsEmpty_ReturnsAllZeros()
    {
        // Arrange — $facet over an empty match yields empty arrays everywhere.
        var facetResult = FacetResult();

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        dto.TotalIncome.Should().Be(0m);
        dto.TotalExpense.Should().Be(0m);
        dto.NetSavings.Should().Be(0m);
        dto.CategorySpent.Should().BeEmpty();
        dto.RecentTransactions.Should().BeEmpty();
        dto.TransactionCount.Should().Be(0);
    }

    [Fact]
    public void Map_IncomeOnlyLedger_YieldsPositiveNetSavings()
    {
        // Arrange
        var facetResult = FacetResult(
            totals: [new BsonDocument { { "_id", 1 }, { "total", new BsonDecimal128(500m) } }],
            count: 3);

        // Act
        var dto = GetDashboardSummaryPipeline.Map(facetResult);

        // Assert
        dto.TotalIncome.Should().Be(500m);
        dto.TotalExpense.Should().Be(0m);
        dto.NetSavings.Should().Be(500m);
    }

    private static BsonDocument FacetResult(
        BsonDocument[]? totals = null,
        BsonDocument[]? categorySpent = null,
        BsonDocument[]? recent = null,
        long? count = null) =>
        new()
        {
            { "totals", new BsonArray(totals ?? []) },
            { "categorySpent", new BsonArray(categorySpent ?? []) },
            { "recent", new BsonArray(recent ?? []) },
            {
                "count", count.HasValue
                    ? new BsonArray { new BsonDocument("value", count.Value) }
                    : new BsonArray()
            }
        };
}
