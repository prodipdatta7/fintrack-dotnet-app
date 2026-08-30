namespace FinTrack.Modules.Assistant.Features.GetPortfolioOrAccountBalance;

public sealed record AccountBalanceItemDto(
    string Id,
    string Name,
    string AccountType,
    decimal Balance,
    string Currency);

public sealed record PortfolioOrAccountBalanceResult(
    decimal TotalBalance,
    string Currency,
    int AccountCount,
    AccountBalanceItemDto? TargetAccount,
    IReadOnlyList<AccountBalanceItemDto> Accounts);
