using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.Modules.Users.Services;

namespace FinTrack.Modules.Users;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IFirebaseUserResolver, FirebaseUserResolver>();
        services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();

        return services;
    }
}