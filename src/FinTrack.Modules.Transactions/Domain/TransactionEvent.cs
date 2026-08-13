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

    // Added later — documents written before these fields existed deserialize to the defaults below.
    public string PerformedBy { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
