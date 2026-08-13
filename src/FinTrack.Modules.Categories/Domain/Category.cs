using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Categories.Domain;

public sealed class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    /// <summary>Monthly spending cap. 0 = no limit.</summary>
    public decimal BudgetLimit { get; set; }
}
