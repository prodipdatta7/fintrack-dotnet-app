using FinTrack.BuildingBlocks.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Transactions.Domain;

[BsonIgnoreExtraElements]
public sealed class Transaction : AuditableEntity
{
    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("type")]
    public TransactionType Type { get; set; }

    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("timeZoneOffsetInMinutes")]
    public int TimeZoneOffsetInMinutes { get; set; }

    [BsonElement("time")]
    public string Time { get; set; } = string.Empty;

    [BsonElement("paymentMethod")]
    public string PaymentMethod { get; set; } = string.Empty;

    [BsonElement("receiptFileName")]
    public string ReceiptFileName { get; set; } = string.Empty;

    [BsonElement("receiptUrl")]
    public string ReceiptUrl { get; set; } = string.Empty;

    [BsonElement("tags")]
    public string Tags { get; set; } = string.Empty;

    [BsonElement("attachments")]
    public List<TransactionAttachment> Attachments { get; set; } = new();
}
