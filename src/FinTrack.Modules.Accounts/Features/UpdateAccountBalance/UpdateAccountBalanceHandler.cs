using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.UpdateAccountBalance;

internal sealed class UpdateAccountBalanceHandler : IRequestHandler<UpdateAccountBalanceCommand, Result>
{
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;

    public UpdateAccountBalanceHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        UpdateAccountBalanceCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts
            .Find(a => a.Id == request.Id && a.UserId == _currentUser.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
            return Result.Failure("Account not found.");

        if (account.IsClosed)
            return Result.Failure("Account is closed.");

        var update = Builders<Account>.Update
            .Set(a => a.Balance, request.Balance)
            .Set(a => a.ModifiedAt, DateTime.UtcNow);

        await _accounts.UpdateOneAsync(
            a => a.Id == request.Id && a.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
