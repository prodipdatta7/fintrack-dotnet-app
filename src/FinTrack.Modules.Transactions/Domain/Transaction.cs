using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Transactions.Domain;

public sealed class Transaction : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public int TimeZoneOffsetInMinutes { get; set; }
}
