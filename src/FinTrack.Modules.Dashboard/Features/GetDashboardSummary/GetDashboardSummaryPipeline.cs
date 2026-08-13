using MongoDB.Bson;

namespace FinTrack.Modules.Dashboard.Features.GetDashboardSummary;

/// <summary>
/// Builds the summary aggregation over the raw <c>transactions</c> collection and maps its
/// result. The Dashboard module reads transactions as <see cref="BsonDocument"/> — it must not
/// reference the Transactions module assembly (PAT-003), so the BSON element names are spelled
/// out here: <c>UserId</c> comes from <c>AuditableEntity</c> (default PascalCase mapping, no
/// camelCase convention pack is registered), while the transaction-specific fields use the
/// camelCase names declared via <c>[BsonElement]</c> on the Transaction domain class.
/// </summary>
internal static class GetDashboardSummaryPipeline
{
    private const int IncomeType = 1;
    private const int ExpenseType = 2;

    /// <summary>
    /// RISK-002: date filtering uses plain UTC boundaries. Query values with an unspecified
    /// kind are treated as UTC rather than being shifted through the server's local timezone.
    /// </summary>
    internal static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    internal static BsonDocument[] BuildStages(
        string userId, DateTime? fromUtc, DateTime? toUtc, string? accountId)
    {
        // SEC-001: every dashboard read is scoped to the caller.
        var match = new BsonDocument { { "UserId", userId } };

        if (fromUtc.HasValue || toUtc.HasValue)
        {
            var range = new BsonDocument();
            if (fromUtc.HasValue)
                range.Add("$gte", fromUtc.Value);
            if (toUtc.HasValue)
                range.Add("$lte", toUtc.Value);
            match.Add("date", range);
        }

        if (!string.IsNullOrWhiteSpace(accountId))
            match.Add("accountId", accountId);

        // REQ-014: everything is computed inside MongoDB in a single $facet pass; the only
        // documents pulled into memory are the 5 recent transactions.
        var facet = new BsonDocument("$facet", new BsonDocument
        {
            {
                "totals", new BsonArray
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$type" },
                        { "total", new BsonDocument("$sum", "$amount") }
                    })
                }
            },
            {
                "categorySpent", new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument("type", ExpenseType)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$categoryId" },
                        { "spent", new BsonDocument("$sum", "$amount") }
                    }),
                    new BsonDocument("$sort", new BsonDocument("spent", -1))
                }
            },
            {
                "recent", new BsonArray
                {
                    new BsonDocument("$sort", new BsonDocument("date", -1)),
                    new BsonDocument("$limit", 5)
                }
            },
            {
                "count", new BsonArray
                {
                    new BsonDocument("$count", "value")
                }
            }
        });

        return [new BsonDocument("$match", match), facet];
    }

    internal static DashboardSummaryDto Empty { get; } =
        new(0m, 0m, 0m, [], [], 0);

    internal static DashboardSummaryDto Map(BsonDocument facetResult)
    {
        var totalIncome = 0m;
        var totalExpense = 0m;

        foreach (var total in GetArray(facetResult, "totals").OfType<BsonDocument>())
        {
            var type = total.GetValue("_id", 0).ToInt32();
            var amount = GetDecimal(total, "total");

            if (type == IncomeType)
                totalIncome = amount;
            else if (type == ExpenseType)
                totalExpense = amount;
        }

        var categorySpent = GetArray(facetResult, "categorySpent")
            .OfType<BsonDocument>()
            .Select(d => new CategorySpentDto(GetId(d), GetDecimal(d, "spent")))
            .ToList();

        var recent = GetArray(facetResult, "recent")
            .OfType<BsonDocument>()
            .Select(MapTransaction)
            .ToList();

        var countDoc = GetArray(facetResult, "count").OfType<BsonDocument>().FirstOrDefault();
        var transactionCount = countDoc is null ? 0L : countDoc.GetValue("value", 0L).ToInt64();

        return new DashboardSummaryDto(
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            categorySpent,
            recent,
            transactionCount);
    }

    internal static DashboardTransactionDto MapTransaction(BsonDocument doc) =>
        new(
            Id: GetId(doc),
            Title: GetString(doc, "title"),
            Amount: GetDecimal(doc, "amount"),
            Type: doc.GetValue("type", 0).ToInt32(),
            CategoryId: GetString(doc, "categoryId"),
            AccountId: GetString(doc, "accountId"),
            Date: doc.TryGetValue("date", out var date) && date.IsValidDateTime
                ? date.ToUniversalTime()
                : default,
            TimeZoneOffsetInMinutes: doc.GetValue("timeZoneOffsetInMinutes", 0).ToInt32(),
            Time: GetString(doc, "time"),
            PaymentMethod: GetString(doc, "paymentMethod"),
            ReceiptFileName: GetString(doc, "receiptFileName"),
            ReceiptUrl: GetString(doc, "receiptUrl"),
            Tags: GetString(doc, "tags"),
            Attachments: MapAttachments(doc));

    private static IReadOnlyList<DashboardAttachmentDto> MapAttachments(BsonDocument doc)
    {
        if (!doc.TryGetValue("attachments", out var value) || !value.IsBsonArray)
            return [];

        return value.AsBsonArray
            .OfType<BsonDocument>()
            .Select(a => new DashboardAttachmentDto(
                GetString(a, "fileName"),
                GetString(a, "fileUrl")))
            .ToList();
    }

    private static string GetId(BsonDocument doc) =>
        doc.TryGetValue("_id", out var id) && !id.IsBsonNull
            ? id.ToString() ?? string.Empty
            : string.Empty;

    private static BsonArray GetArray(BsonDocument doc, string name) =>
        doc.TryGetValue(name, out var value) && value.IsBsonArray ? value.AsBsonArray : new BsonArray();

    private static string GetString(BsonDocument doc, string name) =>
        doc.TryGetValue(name, out var value) && value.IsString ? value.AsString : string.Empty;

    private static decimal GetDecimal(BsonDocument doc, string name)
    {
        // $sum yields Decimal128 for decimal-typed amounts but int 0 for empty groups.
        if (!doc.TryGetValue(name, out var value) || value.IsBsonNull)
            return 0m;

        return value.ToDecimal();
    }
}
