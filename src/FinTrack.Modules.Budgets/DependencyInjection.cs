using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Budgets;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetsModule(this IServiceCollection services)
    {
        return services;
    }
}
