namespace FinTrack.Modules.Assistant.Features.GetCategorySpendingVsBudget;

public sealed record CategorySpendingVsBudgetResult(
    string CategoryId,
    string CategoryName,
    string CategoryType,
    string Period,
    DateTime? FromUtc,
    DateTime? ToUtc,
    decimal TotalSpent,
    int TransactionCount,
    decimal BudgetLimit,
    decimal RemainingBudget,
    bool IsOverBudget,
    decimal PercentageUsed);
