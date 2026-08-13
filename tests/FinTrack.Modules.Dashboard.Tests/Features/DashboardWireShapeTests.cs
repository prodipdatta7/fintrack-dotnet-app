using System.Text.Json;
using FinTrack.Modules.Dashboard.Features.GetCashflowSeries;
using FinTrack.Modules.Dashboard.Features.GetDashboardSummary;
using FluentAssertions;
using Xunit;

namespace FinTrack.Modules.Dashboard.Tests.Features;

/// <summary>
/// Guards the JSON contract the Angular client consumes. ASP.NET Core serializes responses
/// with web defaults (camelCase), which these tests reproduce.
/// </summary>
public class DashboardWireShapeTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DashboardSummaryDto_SerializesToCamelCaseContract()
    {
        // Arrange
        var dto = new DashboardSummaryDto(
            TotalIncome: 1500m,
            TotalExpense: 600m,
            NetSavings: 900m,
            CategorySpent: [new CategorySpentDto("cat-1", 250m)],
            RecentTransactions:
            [
                new DashboardTransactionDto(
                    "tx-1", "Groceries", 84.99m, 2, "cat-1", "acc-1",
                    new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                    Attachments: [new DashboardAttachmentDto("a.png", "/files/a.png")])
            ],
            TransactionCount: 42);

        // Act
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, WebOptions));
        var root = json.RootElement;

        // Assert
        root.GetProperty("totalIncome").GetDecimal().Should().Be(1500m);
        root.GetProperty("totalExpense").GetDecimal().Should().Be(600m);
        root.GetProperty("netSavings").GetDecimal().Should().Be(900m);
        root.GetProperty("transactionCount").GetInt64().Should().Be(42);

        var category = root.GetProperty("categorySpent")[0];
        category.GetProperty("categoryId").GetString().Should().Be("cat-1");
        category.GetProperty("spent").GetDecimal().Should().Be(250m);

        var tx = root.GetProperty("recentTransactions")[0];
        tx.GetProperty("id").GetString().Should().Be("tx-1");
        tx.GetProperty("title").GetString().Should().Be("Groceries");
        tx.GetProperty("amount").GetDecimal().Should().Be(84.99m);
        tx.GetProperty("type").GetInt32().Should().Be(2);
        tx.GetProperty("categoryId").GetString().Should().Be("cat-1");
        tx.GetProperty("accountId").GetString().Should().Be("acc-1");
        tx.TryGetProperty("date", out _).Should().BeTrue();
        tx.TryGetProperty("timeZoneOffsetInMinutes", out _).Should().BeTrue();
        tx.TryGetProperty("time", out _).Should().BeTrue();
        tx.TryGetProperty("paymentMethod", out _).Should().BeTrue();
        tx.TryGetProperty("receiptFileName", out _).Should().BeTrue();
        tx.TryGetProperty("receiptUrl", out _).Should().BeTrue();
        tx.TryGetProperty("tags", out _).Should().BeTrue();
        tx.GetProperty("attachments")[0].GetProperty("fileName").GetString().Should().Be("a.png");
        tx.GetProperty("attachments")[0].GetProperty("fileUrl").GetString().Should().Be("/files/a.png");
    }

    [Fact]
    public void CashflowPointDto_SerializesToCamelCaseContract()
    {
        // Arrange
        var dto = new CashflowPointDto("Aug 12", 120.50m, 45.25m);

        // Act
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(dto, WebOptions));
        var root = json.RootElement;

        // Assert
        root.GetProperty("label").GetString().Should().Be("Aug 12");
        root.GetProperty("income").GetDecimal().Should().Be(120.50m);
        root.GetProperty("expense").GetDecimal().Should().Be(45.25m);
    }
}
