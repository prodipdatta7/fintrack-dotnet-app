using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;

internal sealed class GetCategorySpendingVsBudgetHandler
    : IRequestHandler<GetCategorySpendingVsBudgetQuery, Result<CategorySpendingVsBudgetResult>>
{
    private const int ExpenseType = 2;
    private readonly IMongoCollection<BsonDocument> _categories;
    private readonly IMongoCollection<BsonDocument> _transactions;
    private readonly ICurrentUser _currentUser;

    public GetCategorySpendingVsBudgetHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _categories = database.GetCollection<BsonDocument>("categories");
        _transactions = database.GetCollection<BsonDocument>("transactions");
        _currentUser = currentUser;
    }

    public async Task<Result<CategorySpendingVsBudgetResult>> Handle(
        GetCategorySpendingVsBudgetQuery request, CancellationToken ct)
    {
        var categorySearch = (request.Category ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(categorySearch))
            return Result<CategorySpendingVsBudgetResult>.Failure("Category parameter is required.");

        var userCategories = await _categories.Find(
            Builders<BsonDocument>.Filter.Eq("UserId", _currentUser.UserId)).ToListAsync(ct);

        var matchedCategory = userCategories.FirstOrDefault(c =>
            string.Equals(c.TryGetValue("_id", out var id) ? id.ToString() : null, categorySearch, StringComparison.OrdinalIgnoreCase))
            ?? userCategories.FirstOrDefault(c =>
            string.Equals(c.TryGetValue("Name", out var n) ? n.AsString : null, categorySearch, StringComparison.OrdinalIgnoreCase))
            ?? userCategories.FirstOrDefault(c =>
            c.TryGetValue("Name", out var n) && n.AsString.Contains(categorySearch, StringComparison.OrdinalIgnoreCase));

        var categoryId = matchedCategory?.TryGetValue("_id", out var mId) == true ? mId.ToString() ?? string.Empty : string.Empty;
        var categoryName = matchedCategory?.TryGetValue("Name", out var mName) == true ? mName.AsString : categorySearch;
        var categoryType = matchedCategory?.TryGetValue("Type", out var mType) == true && mType.ToInt32() == 1 ? "Income" : "Expense";
        var budgetLimit = matchedCategory?.TryGetValue("BudgetLimit", out var bLim) == true ? bLim.ToDecimal() : 0m;

        var (fromUtc, toUtc, normalizedPeriod) = ResolvePeriodBounds(request.Period);

        var builder = Builders<BsonDocument>.Filter;
        var txFilter = builder.Eq("UserId", _currentUser.UserId) & builder.Eq("type", ExpenseType);

        if (!string.IsNullOrWhiteSpace(categoryId))
        {
            txFilter &= builder.Eq("categoryId", categoryId);
        }
        else
        {
            // If category is not found by ID, match transactions with this search term in categoryId or title
            txFilter &= builder.Eq("categoryId", categorySearch);
        }

        if (fromUtc.HasValue)
            txFilter &= builder.Gte("date", fromUtc.Value);
        if (toUtc.HasValue)
            txFilter &= builder.Lte("date", toUtc.Value);

        var matchingTransactions = await _transactions.Find(txFilter).ToListAsync(ct);

        var totalSpent = matchingTransactions.Sum(t => t.TryGetValue("amount", out var amt) ? amt.ToDecimal() : 0m);
        var transactionCount = matchingTransactions.Count;

        var remainingBudget = budgetLimit > 0m ? Math.Max(0m, budgetLimit - totalSpent) : 0m;
        var isOverBudget = budgetLimit > 0m && totalSpent > budgetLimit;
        var percentageUsed = budgetLimit > 0m ? Math.Round((totalSpent / budgetLimit) * 100m, 1) : 0m;

        var result = new CategorySpendingVsBudgetResult(
            CategoryId: categoryId,
            CategoryName: categoryName,
            CategoryType: categoryType,
            Period: normalizedPeriod,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            TotalSpent: totalSpent,
            TransactionCount: transactionCount,
            BudgetLimit: budgetLimit,
            RemainingBudget: remainingBudget,
            IsOverBudget: isOverBudget,
            PercentageUsed: percentageUsed);

        return Result<CategorySpendingVsBudgetResult>.Success(result);
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
