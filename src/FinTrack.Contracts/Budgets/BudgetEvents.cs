namespace FinTrack.Contracts.Budgets;

public record BudgetExceededEvent(
    string BudgetId,
    string UserId,
    string CategoryId,
    decimal Limit,
    decimal CurrentSpend,
    DateTime ExceededAt);
