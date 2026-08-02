using FinTrack.BuildingBlocks.Persistence;

namespace FinTrack.Modules.Users.Domain;

public sealed class UserSettings : AuditableEntity
{
    public string Currency { get; set; } = "BDT";
    public string TimeZone { get; set; } = "Asia/Dhaka";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public int DefaultPageSize { get; set; } = 10;
    public bool EmailNotifications { get; set; } = true;
    public bool BudgetAlerts { get; set; } = true;
    public decimal? BudgetAlertThreshold { get; set; }
}
