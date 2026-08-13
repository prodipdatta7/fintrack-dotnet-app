using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.SetAccountStatus;

internal sealed class SetAccountStatusHandler : IRequestHandler<SetAccountStatusCommand, Result>
{
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;

    public SetAccountStatusHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        SetAccountStatusCommand request, CancellationToken cancellationToken)
    {
        var update = Builders<Account>.Update
            .Set(a => a.IsClosed, request.IsClosed)
            .Set(a => a.ModifiedAt, DateTime.UtcNow);

        var result = await _accounts.UpdateOneAsync(
            a => a.Id == request.Id && a.UserId == _currentUser.UserId,
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 0
            ? Result.Failure("Account not found.")
            : Result.Success();
    }
}
