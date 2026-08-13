using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;

namespace FinTrack.BuildingBlocks.Storage;

/// <summary>
/// Stores uploads in a GCS bucket (Cloud Run / production).
/// Configure Storage:Gcs:BucketName (and ADC / workload identity on GCP).
/// </summary>
public sealed class GcsFileStorageService : IFileStorageService
{
    private readonly StorageClient _client;
    private readonly string _bucketName;

    public GcsFileStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["Storage:Gcs:BucketName"]
            ?? throw new InvalidOperationException("Storage:Gcs:BucketName is required when Storage:Provider=Gcs.");
        _client = StorageClient.Create();
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var objectName = $"{folderName.Trim('/')}/{uniqueFileName}";

        await _client.UploadObjectAsync(
            _bucketName,
            objectName,
            contentType: null,
            fileStream,
            cancellationToken: cancellationToken);

        // Public URL shape; bucket may be public-read or fronted by a CDN later.
        return $"https://storage.googleapis.com/{_bucketName}/{objectName}";
    }

    public async Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var objectName = relativePath;
        var marker = $"/{_bucketName}/";
        var idx = relativePath.IndexOf(marker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            objectName = relativePath[(idx + marker.Length)..];
        }
        else if (relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // Unknown URL shape — skip delete rather than failing the request.
            return;
        }
        else
        {
            objectName = relativePath.TrimStart('/');
        }

        try
        {
            await _client.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            // Already gone
        }
    }
}
