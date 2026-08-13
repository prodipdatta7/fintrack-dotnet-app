using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Budgets;

public static class DependencyInjection
{
    public static IServiceCollection AddBudgetsModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
