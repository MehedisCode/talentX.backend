using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly ILocalStoragePathProvider _localStoragePathProvider;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        ILocalStoragePathProvider localStoragePathProvider,
        ILogger<LocalFileStorageService> logger)
    {
        _localStoragePathProvider = localStoragePathProvider;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        IUploadedFile file,
        string storageFileName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageFileName))
        {
            throw new BadRequestException("Storage file name is required.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(basePath, storageFileName));

        if (!fullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid storage key path.");
        }

        var directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using var stream = file.OpenReadStream();
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        // Return a relative key (portable, not OS-absolute)
        return storageFileName;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.CompletedTask;
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(basePath, storageKey));

        if (!fullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("DeleteAsync called with invalid or out-of-bounds key {StorageKey}; skipping.", storageKey);
            return Task.CompletedTask;
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("DeleteAsync called for non-existent file {StorageKey}; skipping.", storageKey);
            return Task.CompletedTask;
        }

        File.Delete(fullPath);
        _logger.LogInformation("Deleted stored file {StorageKey}.", storageKey);
        return Task.CompletedTask;
    }
}
