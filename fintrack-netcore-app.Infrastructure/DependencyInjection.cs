using fintrack_netcore_app.Application.Interfaces;
using fintrack_netcore_app.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace fintrack_netcore_app.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Register infrastructure services, EF Core, Repositories, external API clients here

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Register the Repository as a scoped service
        return services;
    }
}