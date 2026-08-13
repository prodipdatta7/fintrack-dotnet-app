using FinTrack.BuildingBlocks;
using FinTrack.BuildingBlocks.Auth;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.CreateAccount;

internal sealed class CreateAccountHandler : IRequestHandler<CreateAccountCommand, Result<string>>
{
    private readonly IMongoCollection<Account> _accounts;
    private readonly ICurrentUser _currentUser;

    public CreateAccountHandler(IMongoDatabase database, ICurrentUser currentUser)
    {
        _accounts = database.GetCollection<Account>("accounts");
        _currentUser = currentUser;
    }

    public async Task<Result<string>> Handle(
        CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            Name = request.Name,
            AccountType = request.AccountType,
            Balance = request.Balance,
            Currency = request.Currency,
            Icon = request.Icon,
            Provider = request.Provider,
            Color = request.Color,
            IsClosed = false,
            LedgerSynced = true,
            UserId = _currentUser.UserId,
            CreatedBy = _currentUser.Email
        };

        await _accounts.InsertOneAsync(account, cancellationToken: cancellationToken);

        return Result<string>.Success(account.Id);
    }
}
