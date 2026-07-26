using MongoDB.Entities;

namespace FinTrack.BuildingBlocks;

/// <summary>
/// Base entity class that all module entities inherit from.
/// Wraps Mongo.Entities.Entity and adds audit fields.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public string? LastUpdatedBy { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public int TimeZoneOffsetInMinutes { get; set; }
}
