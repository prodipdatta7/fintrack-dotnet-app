using Microsoft.Extensions.DependencyInjection;

namespace fintrack_netcore_app.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services, MediatR handlers, AutoMapper profiles here
        // services.AddMediatR(typeof(DependencyInjection).Assembly);
        // services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        return services;
    }
}


