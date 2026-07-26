using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Dashboard;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardModule(this IServiceCollection services)
    {
        return services;
    }
}
