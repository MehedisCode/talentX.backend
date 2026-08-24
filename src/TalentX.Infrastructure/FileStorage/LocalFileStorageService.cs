using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IConfiguration configuration,
        ILogger<LocalFileStorageService> logger)
    {
        _basePath = Environment.GetEnvironmentVariable("FILE_STORAGE_LOCAL_BASE_PATH")
            ?? configuration["FILE_STORAGE_LOCAL_BASE_PATH"]
            ?? configuration["FileStorage:LocalBasePath"]
            ?? throw new InvalidOperationException("FileStorage:LocalBasePath is not configured.");
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        IUploadedFile file,
        string storageFileName,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_basePath);
        var fullPath = Path.Combine(_basePath, storageFileName);

        await using var stream = file.OpenReadStream();
        await using var fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, cancellationToken);

        // Return a relative key (portable, not OS-absolute)
        return storageFileName;
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storageKey);

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
