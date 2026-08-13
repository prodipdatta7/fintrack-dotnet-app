using System.Globalization;

namespace FinTrack.Modules.Dashboard.Features.GetCashflowSeries;

/// <summary>
/// Computes the bucket plan (date window + dense bucket list) for a cashflow timeframe.
///
/// RISK-002: all bucket boundaries are plain UTC. Transactions store
/// <c>timeZoneOffsetInMinutes</c>, but v1 deliberately ignores it — using the caller's
/// timezone would require per-request <c>$dateToString</c> timezone handling and a decision
/// about which offset wins across a mixed-offset ledger. Revisit if users report off-by-one
/// days near midnight.
/// </summary>
internal static class CashflowBuckets
{
    internal static readonly string[] AllowedTimeframes =
        ["7D", "15D", "30D", "60D", "6M", "1Y", "Custom"];

    /// <summary>TASK-027: Custom ranges are capped at this many days (inclusive span).</summary>
    internal const int MaxCustomRangeDays = 366;

    internal static bool IsAllowedTimeframe(string? timeframe) =>
        timeframe is not null &&
        AllowedTimeframes.Contains(timeframe, StringComparer.OrdinalIgnoreCase);

    internal static bool IsCustom(string? timeframe) =>
        string.Equals(timeframe, "Custom", StringComparison.OrdinalIgnoreCase);

    internal sealed record Bucket(string Key, string Label);

    internal sealed record BucketPlan(
        DateTime StartUtc,
        DateTime EndExclusiveUtc,
        bool UseMonthBuckets,
        IReadOnlyList<Bucket> Buckets);

    /// <summary>
    /// Builds the plan for a validated query. <paramref name="from"/>/<paramref name="to"/>
    /// are only consulted for the Custom timeframe and must both be present there
    /// (guaranteed by GetCashflowSeriesValidator).
    /// </summary>
    internal static BucketPlan Build(string timeframe, DateTime? from, DateTime? to, DateTime utcNow)
    {
        var today = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);

        if (IsCustom(timeframe))
            return DayPlan(AsUtcDate(from!.Value), AsUtcDate(to!.Value));

        return timeframe.ToUpperInvariant() switch
        {
            "7D" => DayPlan(today.AddDays(-6), today),
            "15D" => DayPlan(today.AddDays(-14), today),
            "30D" => DayPlan(today.AddDays(-29), today),
            "60D" => DayPlan(today.AddDays(-59), today),
            "6M" => MonthPlan(today, 6),
            "1Y" => MonthPlan(today, 12),
            _ => throw new ArgumentException($"Unsupported timeframe '{timeframe}'.", nameof(timeframe))
        };
    }

    private static BucketPlan DayPlan(DateTime startDay, DateTime endDayInclusive)
    {
        var buckets = new List<Bucket>();
        for (var day = startDay; day <= endDayInclusive; day = day.AddDays(1))
            buckets.Add(new Bucket(DayKey(day), DayLabel(day)));

        return new BucketPlan(startDay, endDayInclusive.AddDays(1), UseMonthBuckets: false, buckets);
    }

    private static BucketPlan MonthPlan(DateTime todayUtc, int months)
    {
        var currentMonth = new DateTime(todayUtc.Year, todayUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = currentMonth.AddMonths(-(months - 1));

        var buckets = new List<Bucket>();
        for (var month = start; month <= currentMonth; month = month.AddMonths(1))
            buckets.Add(new Bucket(MonthKey(month), MonthLabel(month)));

        return new BucketPlan(start, currentMonth.AddMonths(1), UseMonthBuckets: true, buckets);
    }

    // Keys must line up with the $dateToString formats in GetCashflowSeriesPipeline
    // ("%Y-%m-%d" and "%Y-%m").
    internal static string DayKey(DateTime day) =>
        day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static string MonthKey(DateTime month) =>
        month.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    // TASK-026: "MMM d" for day buckets (e.g. "Aug 12"), "MMM" for month buckets (e.g. "Aug").
    internal static string DayLabel(DateTime day) =>
        day.ToString("MMM d", CultureInfo.InvariantCulture);

    internal static string MonthLabel(DateTime month) =>
        month.ToString("MMM", CultureInfo.InvariantCulture);

    private static DateTime AsUtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}
