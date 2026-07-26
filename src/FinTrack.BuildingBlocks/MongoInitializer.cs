using MongoDB.Entities;

namespace FinTrack.BuildingBlocks;

/// <summary>
/// Helper for initializing MongoDB in a consistent way across Api and Host.
/// </summary>
public static class MongoInitializer
{
    /// <summary>
    /// Initialize MongoDB connection. Call once per process.
    /// </summary>
    public static async Task InitializeAsync(string connectionString, string databaseName)
    {
        await DB.InitAsync(databaseName, MongoDB.Driver.MongoClientSettings.FromConnectionString(connectionString));
    }
}
