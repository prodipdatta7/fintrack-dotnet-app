using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Accounts.Domain;
using MassTransit;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.EventHandlers;

public sealed class SeedDefaultAccountsConsumer : IConsumer<UserRegistered>
{
    private readonly IMongoCollection<Account> _accounts;

    public SeedDefaultAccountsConsumer(IMongoDatabase database)
    {
        _accounts = database.GetCollection<Account>("accounts");
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var evt = context.Message;

        var defaultAccounts = new List<Account>
        {
            new() { Name = "Bank Account", AccountType = "Bank", Balance = 0, Currency = "BDT", Icon = "🏦", Provider = "", Color = "#3498db", UserId = evt.UserId, CreatedBy = "system", LedgerSynced = true },
            new() { Name = "bKash Wallet", AccountType = "MFS", Balance = 0, Currency = "BDT", Icon = "/providers/bkash.svg", Provider = "bKash", Color = "#E2136E", UserId = evt.UserId, CreatedBy = "system", LedgerSynced = true },
            new() { Name = "Nagad Wallet", AccountType = "MFS", Balance = 0, Currency = "BDT", Icon = "/providers/nagad.svg", Provider = "Nagad", Color = "#F6921E", UserId = evt.UserId, CreatedBy = "system", LedgerSynced = true },
            new() { Name = "Cash in Hand", AccountType = "Cash", Balance = 0, Currency = "BDT", Icon = "/providers/cash.svg", Provider = "Cash", Color = "#2ECC71", UserId = evt.UserId, CreatedBy = "system", LedgerSynced = true }
        };

        await _accounts.InsertManyAsync(defaultAccounts, cancellationToken: context.CancellationToken);
    }
}
