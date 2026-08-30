using FinTrack.BuildingBlocks.Behaviors;
using FinTrack.Modules.Accounts;
using FinTrack.Modules.Assistant;
using FinTrack.Modules.Budgets;
using FinTrack.Modules.Categories;
using FinTrack.Modules.Dashboard;
using FinTrack.Modules.Transactions;
using FinTrack.Modules.Users;
using MediatR;

namespace FinTrack.Api.Extensions;

public static class ModuleExtensions
{
    public static IServiceCollection AddApplicationModules(this IServiceCollection services)
    {
        services.AddUsersModule();
        services.AddTransactionsModule();
        services.AddCategoriesModule();
        services.AddDashboardModule();
        services.AddBudgetsModule();
        services.AddAccountsModule();
        services.AddAssistantModule();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    public static IServiceCollection AddModuleControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(FinTrack.Modules.Users.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Transactions.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Dashboard.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Budgets.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly)
            .AddApplicationPart(typeof(FinTrack.Modules.Assistant.DependencyInjection).Assembly);

        return services;
    }
}
