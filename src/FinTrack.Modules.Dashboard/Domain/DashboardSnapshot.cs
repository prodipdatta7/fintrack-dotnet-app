using FinTrack.BuildingBlocks;

using MongoDB.Entities;

namespace FinTrack.Modules.Dashboard.Domain;

[Collection("dashboardSnapshots")]
public class DashboardSnapshot : Entity
{
    public string UserId { get; set; } = string.Empty;
    public decimal TotalBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetSavings { get; set; }
    public int TransactionCount { get; set; }
    public int AccountCount { get; set; }
    public int CategoryCount { get; set; }
    public int ActiveBudgetCount { get; set; }
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow;
}
