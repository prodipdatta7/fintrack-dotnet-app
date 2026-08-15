using FinTrack.BuildingBlocks.Auth;
using FinTrack.BuildingBlocks.Persistence;
using FinTrack.BuildingBlocks.Storage;
using MassTransit;

namespace FinTrack.Api.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddMongoDb(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        var storageProvider = configuration["Storage:Provider"] ?? "Local";
        if (string.Equals(storageProvider, "Gcs", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorageService, GcsFileStorageService>();
        }
        else
        {
            services.AddScoped<IFileStorageService, LocalFileStorageService>();
        }

        var useMongoOutbox = configuration.GetValue("MassTransit:UseMongoOutbox", true);
        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumers(typeof(FinTrack.Modules.Categories.DependencyInjection).Assembly);
            cfg.AddConsumers(typeof(FinTrack.Modules.Accounts.DependencyInjection).Assembly);

            cfg.UsingInMemory((context, busConfig) =>
            {
                busConfig.ConfigureEndpoints(context);
            });

            if (useMongoOutbox)
            {
                cfg.AddMongoDbOutbox(outbox =>
                {
                    outbox.ClientFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoClient>());
                    outbox.DatabaseFactory(sp => sp.GetRequiredService<MongoDB.Driver.IMongoDatabase>());
                    outbox.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
                    outbox.UseBusOutbox();
                });
            }
        });

        return services;
    }
}
