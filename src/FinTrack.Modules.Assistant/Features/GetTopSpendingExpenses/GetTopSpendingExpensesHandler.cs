using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;

internal sealed class GetTopSpendingExpensesHandler
    : IRequestHandler<GetTopSpendingExpensesQuery, Result<TopSpendingExpensesResult>>
{
    private const int ExpenseType = 2;
    private readonly IMongoCollection<BsonDocument> _transactions;
    private readonly IMongoCollection<BsonDocument> _categories;
    private readonly IMongoCollection<BsonDocument> _accounts;
    private readonly ICurrentUser _currentUser;

    public GetTopSpendingExpensesHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _transactions = database.GetCollection<BsonDocument>("transactions");
        _categories = database.GetCollection<BsonDocument>("categories");
        _accounts = database.GetCollection<BsonDocument>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<TopSpendingExpensesResult>> Handle(
        GetTopSpendingExpensesQuery request, CancellationToken ct)
    {
        var (fromUtc, toUtc, normalizedPeriod) = ResolvePeriodBounds(request.Period);
        var effectiveLimit = Math.Clamp(request.Limit <= 0 ? 5 : request.Limit, 1, 50);

        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("UserId", _currentUser.UserId) & builder.Eq("type", ExpenseType);

        if (fromUtc.HasValue)
            filter &= builder.Gte("date", fromUtc.Value);
        if (toUtc.HasValue)
            filter &= builder.Lte("date", toUtc.Value);

        var expenseDocs = await _transactions.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("amount"))
            .Limit(effectiveLimit)
            .ToListAsync(ct);

        // Fetch categories & accounts for lookup
        var userCategories = await _categories.Find(Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId)).ToListAsync(ct);
        var categoryMap = userCategories.ToDictionary(
            c => c.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
            c => c.TryGetValue("Name", out var n) ? n.AsString : string.Empty);

        var userAccounts = await _accounts.Find(Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId)).ToListAsync(ct);
        var accountMap = userAccounts.ToDictionary(
            a => a.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
            a => a.TryGetValue("Name", out var n) ? n.AsString : string.Empty);

        var topExpenses = expenseDocs.Select(doc =>
        {
            var catId = doc.TryGetValue("categoryId", out var cId) ? cId.AsString : string.Empty;
            var accId = doc.TryGetValue("accountId", out var aId) ? aId.AsString : string.Empty;

            return new TopExpenseItemDto(
                TransactionId: doc.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
                Title: doc.TryGetValue("title", out var t) ? t.AsString : string.Empty,
                Amount: doc.TryGetValue("amount", out var amt) ? amt.ToDecimal() : 0m,
                CategoryId: catId,
                CategoryName: categoryMap.TryGetValue(catId, out var catName) ? catName : catId,
                AccountId: accId,
                AccountName: accountMap.TryGetValue(accId, out var accName) ? accName : accId,
                Date: doc.TryGetValue("date", out var d) && d.IsValidDateTime ? d.ToUniversalTime() : DateTime.UtcNow,
                Note: doc.TryGetValue("note", out var note) ? note.AsString : string.Empty);
        }).ToList();

        var totalSpentInPeriod = topExpenses.Sum(e => e.Amount);

        var result = new TopSpendingExpensesResult(
            Period: normalizedPeriod,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Limit: effectiveLimit,
            TotalSpentInPeriod: totalSpentInPeriod,
            Expenses: topExpenses);

        return Result<TopSpendingExpensesResult>.Success(result);
    }

    private static (DateTime? FromUtc, DateTime? ToUtc, string NormalizedPeriod) ResolvePeriodBounds(string? period)
    {
        var now = DateTime.UtcNow;
        var p = (period ?? "this_month").Trim().ToLowerInvariant();

        return p switch
        {
            "this_week" or "week" => (
                now.Date.AddDays(-(int)now.DayOfWeek),
                now,
                "this_week"),
            "last_month" => (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(-1),
                "last_month"),
            "this_year" or "year" => (
                new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                now,
                "this_year"),
            "all" or "all_time" => (
                null,
                null,
                "all"),
            _ => (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                now,
                "this_month")
        };
    }
}
