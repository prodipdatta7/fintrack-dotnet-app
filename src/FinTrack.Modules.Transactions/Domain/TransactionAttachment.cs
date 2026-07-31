using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Transactions.Domain;

public sealed class TransactionAttachment
{
    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("fileUrl")]
    public string FileUrl { get; set; } = string.Empty;
}
