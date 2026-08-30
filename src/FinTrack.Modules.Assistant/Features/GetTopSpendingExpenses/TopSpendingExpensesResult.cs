namespace FinTrack.Modules.Assistant.Features.GetTopSpendingExpenses;

public sealed record TopExpenseItemDto(
    string TransactionId,
    string Title,
    decimal Amount,
    string CategoryId,
    string CategoryName,
    string AccountId,
    string AccountName,
    DateTime Date,
    string Note);

public sealed record TopSpendingExpensesResult(
    string Period,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Limit,
    decimal TotalSpentInPeriod,
    IReadOnlyList<TopExpenseItemDto> Expenses);
