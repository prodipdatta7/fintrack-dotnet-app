namespace FinTrack.Modules.Transactions.Domain;

public class TransactionEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TransactionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    public string DataJson { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
