namespace FinTrack.Modules.Assistant.Features.GetActiveAccountsSummary;

public sealed record AccountSummaryItemDto(
    string Id,
    string Name,
    string AccountType,
    decimal Balance,
    string Currency,
    string Color,
    bool IsClosed);

public sealed record ActiveAccountsSummaryResult(
    decimal TotalPortfolioBalance,
    int ActiveAccountCount,
    IReadOnlyList<AccountSummaryItemDto> Accounts);
