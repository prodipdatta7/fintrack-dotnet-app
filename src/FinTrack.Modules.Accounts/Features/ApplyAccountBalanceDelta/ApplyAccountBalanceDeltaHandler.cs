using FinTrack.Contracts.Commands;
using FinTrack.Modules.Accounts.Domain;
using FinTrack.Modules.Accounts.EventHandlers;
using MediatR;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.Features.ApplyAccountBalanceDelta;

/// <summary>
/// Synchronous balance projection — called from transaction create/update/delete handlers
/// so the HTTP response already reflects the new balance (MassTransit outbox is too late / unreliable here).
/// </summary>
public sealed class ApplyAccountBalanceDeltaHandler : IRequestHandler<ApplyAccountBalanceDeltaCommand>
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<Account> _accounts;

    public ApplyAccountBalanceDeltaHandler(IMongoDatabase database)
    {
        _database = database;
        _accounts = database.GetCollection<Account>("accounts");
    }

    public async Task Handle(ApplyAccountBalanceDeltaCommand request, CancellationToken cancellationToken)
    {
        if (request.SignedDelta == 0m ||
            string.IsNullOrWhiteSpace(request.AccountId) ||
            string.IsNullOrWhiteSpace(request.UserId))
        {
            return;
        }

        // Unsynced accounts: rebuild from the full ledger (includes the row just written) and stop.
        var backfilled = await AccountLedgerBackfill.EnsureSyncedAsync(
            _database,
            _accounts,
            request.AccountId,
            request.UserId,
            cancellationToken);
        if (backfilled)
            return;

        var update = Builders<Account>.Update
            .Inc(a => a.Balance, request.SignedDelta)
            .Set(a => a.ModifiedAt, DateTime.UtcNow);

        await _accounts.UpdateOneAsync(
            a => a.Id == request.AccountId && a.UserId == request.UserId && !a.IsClosed && a.LedgerSynced,
            update,
            cancellationToken: cancellationToken);
    }

    public static decimal SignedDelta(decimal amount, string type) =>
        string.Equals(type, "Income", StringComparison.OrdinalIgnoreCase) ? amount : -amount;
}
