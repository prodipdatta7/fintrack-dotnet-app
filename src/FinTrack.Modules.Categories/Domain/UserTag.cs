using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Categories.Domain;

/// <summary>A global, user-scoped tag label (e.g. "Tax-Deductible").</summary>
public sealed class UserTag : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Lowercased key used by the case-insensitive unique index.</summary>
    public string NormalizedName { get; set; } = string.Empty;
}