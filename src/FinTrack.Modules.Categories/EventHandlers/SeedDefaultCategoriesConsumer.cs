using FinTrack.Contracts.IntegrationEvents;
using FinTrack.Modules.Categories.Domain;
using MassTransit;
using MongoDB.Driver;

namespace FinTrack.Modules.Categories.EventHandlers;

public sealed class SeedDefaultCategoriesConsumer : IConsumer<UserRegistered>
{
    private readonly IMongoCollection<Category> _categories;

    public SeedDefaultCategoriesConsumer(IMongoDatabase database)
    {
        _categories = database.GetCollection<Category>("categories");
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var evt = context.Message;

        var defaultCategories = new List<Category>
        {
            new() { Name = "Salary", NormalizedName = "salary", Type = CategoryType.Income, Icon = "cash", Color = "#2ecc71", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" },
            new() { Name = "Investments", NormalizedName = "investments", Type = CategoryType.Income, Icon = "trending-up", Color = "#27ae60", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" },
            new() { Name = "Food & Dining", NormalizedName = "food & dining", Type = CategoryType.Expense, Icon = "utensils", Color = "#e74c3c", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" },
            new() { Name = "Transportation", NormalizedName = "transportation", Type = CategoryType.Expense, Icon = "car", Color = "#e67e22", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" },
            new() { Name = "Housing & Utilities", NormalizedName = "housing & utilities", Type = CategoryType.Expense, Icon = "home", Color = "#3498db", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" },
            new() { Name = "Entertainment", NormalizedName = "entertainment", Type = CategoryType.Expense, Icon = "film", Color = "#9b59b6", IsDefault = true, BudgetLimit = 0, UserId = evt.UserId, CreatedBy = "system" }
        };

        await _categories.InsertManyAsync(defaultCategories, cancellationToken: context.CancellationToken);
    }
}