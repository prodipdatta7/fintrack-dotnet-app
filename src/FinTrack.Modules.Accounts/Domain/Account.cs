using FinTrack.BuildingBlocks.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Accounts.Domain;

[BsonIgnoreExtraElements]
public sealed class Account : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // Bank, MFS, Cash, Credit
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BDT";
    public string Icon { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public bool IsClosed { get; set; }

    /// <summary>
    /// Account-only migration flag (not part of transaction events).
    /// False means this account still needs a one-time rebuild from historical ledger rows;
    /// true means live <c>$inc</c> projection owns subsequent TransactionCreated/Updated/Deleted.
    /// </summary>
    public bool LedgerSynced { get; set; }
}
