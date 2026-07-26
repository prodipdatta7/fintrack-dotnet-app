using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Categories;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesModule(this IServiceCollection services)
    {
        return services;
    }
}
