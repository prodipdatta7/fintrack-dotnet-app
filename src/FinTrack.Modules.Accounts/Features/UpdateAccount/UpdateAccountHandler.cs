using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.UpdateAccount;

internal sealed class UpdateAccountHandler : IRequestHandler<UpdateAccountCommand, Result>
{
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts
            .Find(a => a.Id == request.Id && a.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result.Failure("Account not found.");

        var update = Builders<Account>.Update
            .Set(a => a.Name, request.Name)
            .Set(a => a.AccountType, request.AccountType)
            .Set(a => a.Currency, request.Currency)
            .Set(a => a.Icon, request.Icon)
            .Set(a => a.Provider, request.Provider)
            .Set(a => a.Color, request.Color)
            .Set(a => a.ModifiedAt, DateTime.UtcNow);

        await _accounts.UpdateOneAsync(
            a => a.Id == request.Id && a.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
