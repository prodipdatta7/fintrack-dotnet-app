using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using FinTrack.Modules.Accounts.Features.GetAccounts;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.GetAccount;

internal sealed class GetAccountHandler : IRequestHandler<GetAccountQuery, Result<AccountDto>>
{
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;

    public GetAccountHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<AccountDto>> Handle(
        GetAccountQuery request, CancellationToken cancellationToken)
    {
        // Closed accounts stay reachable — the detail page renders them read-only.
        var account = await _accounts
            .Find(a => a.Id == request.Id && a.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result<AccountDto>.Failure("Account not found.");

        var dto = new AccountDto(
            account.Id,
            account.Name,
            account.AccountType,
            account.Balance,
            account.Currency,
            account.Icon,
            account.Provider,
            account.Color,
            account.IsClosed,
            account.CreatedAt);

        return Result<AccountDto>.Success(dto);
    }
}
