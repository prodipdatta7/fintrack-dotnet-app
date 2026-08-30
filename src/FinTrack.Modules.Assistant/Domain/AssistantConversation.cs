using FinTrack.BuildingBlocks.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Assistant.Domain;

[BsonIgnoreExtraElements]
public sealed class AssistantConversation : AuditableEntity
{
    public string Title { get; set; } = "New Conversation";
    public bool IsPinned { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
}
