using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Categories.Domain;

/// <summary>
/// Join document binding a user-scoped tag to a category. A single tag may be
/// associated with many categories (one join document per category).
/// </summary>
public sealed class CategoryTag : AuditableEntity
{
    public string CategoryId { get; set; } = string.Empty;

    /// <summary>Reference to the canonical <see cref="UserTag"/> (dedupe key).</summary>
    public string TagId { get; set; } = string.Empty;

    /// <summary>Denormalized tag name for display; resolves from the canonical tag.</summary>
    public string Name { get; set; } = string.Empty;
}