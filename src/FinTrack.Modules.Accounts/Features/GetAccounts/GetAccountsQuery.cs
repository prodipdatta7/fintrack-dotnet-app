using FinTrack.BuildingBlocks;
using MediatR;

namespace FinTrack.Modules.Accounts.Features.GetAccounts;

public sealed record GetAccountsQuery(bool IncludeClosed = false) : IRequest<Result<AccountListResult>>;

public sealed record AccountDto(
    string Id,
    string Name,
    string AccountType,
    decimal Balance,
    string Currency,
    string Icon,
    string Provider,
    string Color,
    bool IsClosed,
    DateTime CreatedAt);

public sealed record AccountListResult(List<AccountDto> Items, decimal TotalBalance);
