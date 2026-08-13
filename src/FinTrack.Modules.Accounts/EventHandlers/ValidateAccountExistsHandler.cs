using FinTrack.Contracts.Queries;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.EventHandlers;

internal sealed class ValidateAccountExistsHandler : IRequestHandler<ValidateAccountExistsQuery, bool>
{
    private readonly IMongoCollection<Account> _accounts;

    public ValidateAccountExistsHandler(IMongoDatabase database)
    {
        _accounts = database.GetCollection<Account>("accounts");
    }

    public async Task<bool> Handle(ValidateAccountExistsQuery request, CancellationToken cancellationToken)
    {
        return await _accounts
            .Find(a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsClosed)
            .AnyAsync(cancellationToken);
    }
}
