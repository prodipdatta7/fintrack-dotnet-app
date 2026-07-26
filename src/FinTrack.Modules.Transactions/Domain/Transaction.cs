using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Transactions.Domain;

[Collection("transactions")]
public class Transaction : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
}
