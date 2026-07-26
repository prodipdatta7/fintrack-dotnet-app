using FinTrack.BuildingBlocks;
using FinTrack.Modules.Accounts.Domain;
using MediatR;
using MongoDB.Entities;

namespace FinTrack.Modules.Accounts.Features.CreateAccount;

public record CreateAccountCommand(
    string Name,
    AccountType Type,
    decimal InitialBalance = 0) : IRequest<string>;

internal sealed class CreateAccountHandler : IRequestHandler<CreateAccountCommand, string>
{
    private readonly ICurrentUser _currentUser;

    public CreateAccountHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<string> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated.");

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Type = request.Type,
            Balance = request.InitialBalance,
            IsClosed = false,
            CreatedBy = userId,
            CreateDate = DateTime.UtcNow
        };

        await account.SaveAsync(cancellation: cancellationToken);
        return account.ID;
    }
}
