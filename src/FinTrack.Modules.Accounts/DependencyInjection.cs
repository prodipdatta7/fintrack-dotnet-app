using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Accounts;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        return services;
    }
}
