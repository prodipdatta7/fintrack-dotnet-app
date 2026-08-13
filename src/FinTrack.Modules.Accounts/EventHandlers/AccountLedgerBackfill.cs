using FinTrack.Modules.Accounts.Domain;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinTrack.Modules.Accounts.EventHandlers;

/// <summary>
/// One-time repair for accounts that still hold only an opening/manual balance because
/// transactions were recorded before the live balance projection existed.
/// <para>
/// After backfill, <see cref="Account.LedgerSynced"/> is true and live
/// <c>ApplyAccountBalanceDelta</c> commands use per-event <c>$inc</c> only.
/// </para>
/// </summary>
internal static class AccountLedgerBackfill
{
    private const int IncomeType = 1;

    public static async Task ApplyIfNeededAsync(
        IMongoDatabase database,
        IMongoCollection<Account> accounts,
        string userId,
        List<Account> loaded,
        CancellationToken cancellationToken)
    {
        var pending = loaded.Where(account => !account.LedgerSynced).ToList();
        if (pending.Count == 0)
            return;

        var deltas = await SumSignedDeltasByAccountAsync(
            database,
            userId,
            pending.Select(account => account.Id).ToArray(),
            cancellationToken);

        foreach (var account in pending)
        {
            deltas.TryGetValue(account.Id, out var delta);
            await MarkSyncedAsync(accounts, account, userId, delta, cancellationToken);
        }
    }

    /// <summary>
    /// Syncs one account from the full ledger. Returns <c>true</c> when a backfill ran
    /// (caller must not also apply the triggering event's delta — it is already in the sum).
    /// </summary>
    public static async Task<bool> EnsureSyncedAsync(
        IMongoDatabase database,
        IMongoCollection<Account> accounts,
        string accountId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(userId))
            return false;

        var account = await accounts
            .Find(a => a.Id == accountId && a.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null || account.IsClosed || account.LedgerSynced)
            return false;

        var deltas = await SumSignedDeltasByAccountAsync(
            database,
            userId,
            [accountId],
            cancellationToken);
        deltas.TryGetValue(accountId, out var delta);

        await MarkSyncedAsync(accounts, account, userId, delta, cancellationToken);
        return true;
    }

    private static async Task MarkSyncedAsync(
        IMongoCollection<Account> accounts,
        Account account,
        string userId,
        decimal delta,
        CancellationToken cancellationToken)
    {
        var update = Builders<Account>.Update
            .Inc(a => a.Balance, delta)
            .Set(a => a.LedgerSynced, true)
            .Set(a => a.ModifiedAt, DateTime.UtcNow);

        await accounts.UpdateOneAsync(
            a => a.Id == account.Id && a.UserId == userId && !a.LedgerSynced,
            update,
            cancellationToken: cancellationToken);

        account.Balance += delta;
        account.LedgerSynced = true;
    }

    private static async Task<Dictionary<string, decimal>> SumSignedDeltasByAccountAsync(
        IMongoDatabase database,
        string userId,
        IReadOnlyCollection<string> accountIds,
        CancellationToken cancellationToken)
    {
        if (accountIds.Count == 0)
            return new Dictionary<string, decimal>();

        var transactions = database.GetCollection<BsonDocument>("transactions");
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "UserId", userId },
                { "accountId", new BsonDocument("$in", new BsonArray(accountIds)) }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$accountId" },
                {
                    "delta",
                    new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { "$type", IncomeType }),
                        "$amount",
                        new BsonDocument("$multiply", new BsonArray { "$amount", -1 })
                    }))
                }
            })
        };

        var rows = await transactions.Aggregate<BsonDocument>(pipeline)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => row["_id"].AsString,
            row => row["delta"].ToDecimal());
    }
}
