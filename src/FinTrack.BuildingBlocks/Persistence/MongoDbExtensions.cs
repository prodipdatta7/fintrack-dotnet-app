using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace FinTrack.BuildingBlocks.Persistence;

public static class MongoDbExtensions
{
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["MongoDb:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "MongoDb:ConnectionString is not configured. " +
                "Set the MongoDb__ConnectionString environment variable (Atlas 'mongodb+srv://...', without ?directConnection=true) " +
                "or configure local MongoDB (see docker/phase1/docker-compose.yml).");
        }

        var databaseName = configuration["MongoDb:DatabaseName"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = "FinTrackDb";
        }

        MongoClientSettings settings;
        try
        {
            settings = MongoClientSettings.FromConnectionString(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Invalid MongoDb:ConnectionString value ({Redact(connectionString)}). " +
                "For Atlas use 'mongodb+srv://<user>:<pass>@<cluster>.mongodb.net/?retryWrites=true&w=majority' (no ?directConnection=true).", ex);
        }

        // Fail fast instead of hanging 30s when MongoDB is down/misconfigured.
        var timeoutSeconds = configuration.GetValue("MongoDb:ServerSelectionTimeoutSeconds", 5);
        timeoutSeconds = Math.Clamp(timeoutSeconds, 1, 30);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(timeoutSeconds);

        services.AddSingleton<IMongoClient>(_ => new MongoClient(settings));
        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        return services;
    }

    private static string Redact(string connectionString)
    {
        try
        {
            var url = new MongoUrl(connectionString);
            return url.Server.ToString();
        }
        catch
        {
            return "<unparseable>";
        }
    }
}
