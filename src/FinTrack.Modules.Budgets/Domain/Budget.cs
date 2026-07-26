using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Budgets.Domain;

[Collection("budgets")]
public class Budget : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal CurrentSpend { get; set; }
    public string Period { get; set; } = "Monthly"; // Monthly, Weekly, Yearly
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
