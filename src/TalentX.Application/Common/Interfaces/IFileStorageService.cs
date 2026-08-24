namespace TalentX.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves the file and returns the storage key (relative path or identifier).
    /// </summary>
    Task<string> SaveAsync(IUploadedFile file, string storageFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file identified by the given storage key.
    /// Implementations must be tolerant of keys that no longer exist (no-op / log &amp; swallow).
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
