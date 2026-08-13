using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Budgets.Domain;

public sealed class SavingsPlan : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string Color { get; set; } = "#6366f1";
    public DateTime Deadline { get; set; }
}
