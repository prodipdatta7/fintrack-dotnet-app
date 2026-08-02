namespace FinTrack.BuildingBlocks.Storage;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a file stream under a specified folder and returns the public relative URL.
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file given its relative URL or path.
    /// </summary>
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
}
