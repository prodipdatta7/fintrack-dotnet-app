using FinTrack.BuildingBlocks.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace FinTrack.Modules.Assistant.Domain;

[BsonIgnoreExtraElements]
public sealed class AssistantMessage : AuditableEntity
{
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // "user" | "assistant" | "system" | "tool"
    public string Content { get; set; } = string.Empty;
    public string? ActionType { get; set; }
    public string? ActionStatus { get; set; }
    public string? ActionSummary { get; set; }
    public string? ActionPayloadJson { get; set; }
    public string? ToolCallJson { get; set; }
    public string? ToolResultJson { get; set; }
}
