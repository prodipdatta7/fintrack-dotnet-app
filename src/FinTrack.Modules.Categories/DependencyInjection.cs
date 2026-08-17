using FinTrack.Modules.Categories.Infrastructure;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Categories;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddHostedService<CategoryDataInitializer>();

        return services;
    }
}
