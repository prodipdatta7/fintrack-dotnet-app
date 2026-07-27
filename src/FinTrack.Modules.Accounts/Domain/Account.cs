using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Accounts.Domain;

public sealed class Account : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty; // Cash, Bank, Wallet, Credit
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsClosed { get; set; }
}
