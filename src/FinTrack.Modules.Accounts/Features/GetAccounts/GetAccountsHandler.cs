using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using FinTrack.Modules.Accounts.EventHandlers;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.GetAccounts;

internal sealed class GetAccountsHandler : IRequestHandler<GetAccountsQuery, Result<AccountListResult>>
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;
    private static bool _indexCreated;

    public GetAccountsHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _database = database;
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;

        if (!_indexCreated)
        {
            var indexKeys = Builders<Account>.IndexKeys.Ascending(a => a.UserId);
            _accounts.Indexes.CreateOne(new CreateIndexModel<Account>(indexKeys));
            _indexCreated = true;
        }
    }

    public async Task<Result<AccountListResult>> Handle(
        GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var builder = Builders<Account>.Filter;
        var filter = builder.Eq(a => a.UserId, _currentUser.UserId);

        if (!request.IncludeClosed)
            filter &= builder.Eq(a => a.IsClosed, false);

        var accounts = await _accounts.Find(filter)
            .SortBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        await AccountLedgerBackfill.ApplyIfNeededAsync(
            _database,
            _accounts,
            _currentUser.UserId,
            accounts,
            cancellationToken);

        var items = accounts.Select(a => new AccountDto(
            a.Id,
            a.Name,
            a.AccountType,
            a.Balance,
            a.Currency,
            a.Icon,
            a.Provider,
            a.Color,
            a.IsClosed,
            a.CreatedAt)).ToList();

        // Portfolio total always reflects open accounts only, even when closed ones are listed.
        var totalBalance = accounts.Where(a => !a.IsClosed).Sum(a => a.Balance);

        return Result<AccountListResult>.Success(new AccountListResult(items, totalBalance));
    }
}
