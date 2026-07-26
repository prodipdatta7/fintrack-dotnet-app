using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Accounts.Domain;

[Collection("accounts")]
public class Account : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public bool IsClosed { get; set; }
}

public enum AccountType
{
    Bank = 1,
    Cash = 2,
    Wallet = 3
}
