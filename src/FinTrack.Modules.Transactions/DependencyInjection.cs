using Microsoft.Extensions.DependencyInjection;

namespace FinTrack.Modules.Transactions;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services)
    {
        return services;
    }
}
