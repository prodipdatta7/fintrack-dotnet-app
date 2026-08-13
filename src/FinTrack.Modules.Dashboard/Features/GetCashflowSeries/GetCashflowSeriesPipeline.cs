using MongoDB.Bson;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

/// <summary>
/// Builds the cashflow aggregation ($match + $group per REQ-014) and zero-fills the grouped
/// result into the dense series the client expects (REQ-015). Field names follow the
/// Transactions module's BSON mapping — see GetDashboardSummaryPipeline for the rationale.
/// </summary>
internal static class GetCashflowSeriesPipeline
{
    private const int IncomeType = 1;
    private const int ExpenseType = 2;

    internal static BsonDocument[] BuildStages(
        string userId, CashflowBuckets.BucketPlan plan, string? accountId)
    {
        // SEC-001: scoped to the caller. RISK-002: window boundaries are UTC.
        var match = new BsonDocument
        {
            { "UserId", userId },
            {
                "date", new BsonDocument
                {
                    { "$gte", plan.StartUtc },
                    { "$lt", plan.EndExclusiveUtc }
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(accountId))
            match.Add("accountId", accountId);

        // $dateToString without a timezone argument formats in UTC, matching the bucket keys
        // produced by CashflowBuckets (RISK-002: UTC bucketing for v1, documented there).
        var format = plan.UseMonthBuckets ? "%Y-%m" : "%Y-%m-%d";

        var group = new BsonDocument("$group", new BsonDocument
        {
            {
                "_id", new BsonDocument("$dateToString", new BsonDocument
                {
                    { "format", format },
                    { "date", "$date" }
                })
            },
            { "income", SumWhereType(IncomeType) },
            { "expense", SumWhereType(ExpenseType) }
        });

        return [new BsonDocument("$match", match), group];
    }

    private static BsonDocument SumWhereType(int type) =>
        new("$sum", new BsonDocument("$cond", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray { "$type", type }),
            "$amount",
            0
        }));

    /// <summary>
    /// REQ-015: buckets with no transactions return zero — never fabricated values.
    /// </summary>
    internal static List<CashflowPointDto> MapSeries(
        CashflowBuckets.BucketPlan plan, IEnumerable<BsonDocument> groups)
    {
        var byKey = groups.ToDictionary(
            g => g.GetValue("_id", BsonNull.Value)?.ToString() ?? string.Empty,
            g => (Income: GetDecimal(g, "income"), Expense: GetDecimal(g, "expense")));

        return plan.Buckets
            .Select(b => byKey.TryGetValue(b.Key, out var v)
                ? new CashflowPointDto(b.Label, v.Income, v.Expense)
                : new CashflowPointDto(b.Label, 0m, 0m))
            .ToList();
    }

    private static decimal GetDecimal(BsonDocument doc, string name)
    {
        // $sum yields Decimal128 for decimal-typed amounts but int 0 when only the
        // other branch of the $cond matched.
        if (!doc.TryGetValue(name, out var value) || value.IsBsonNull)
            return 0m;

        return value.ToDecimal();
    }
}
