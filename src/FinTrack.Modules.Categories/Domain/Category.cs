using FinTrack.BuildingBlocks;
using MongoDB.Entities;

namespace FinTrack.Modules.Categories.Domain;

[Collection("categories")]
public class Category : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
}

public enum CategoryType
{
    Income = 1,
    Expense = 2
}
