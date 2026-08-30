using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.FileStorage;

public sealed class LocalMultipartStorageService : IMultipartStorageService
{
    private const int DefaultPartSizeBytes = 5 * 1024 * 1024;
    private readonly string _baseUrl;
    private readonly ILocalStoragePathProvider _localStoragePathProvider;
    private readonly ILogger<LocalMultipartStorageService> _logger;

    public LocalMultipartStorageService(
        IConfiguration configuration,
        ILocalStoragePathProvider localStoragePathProvider,
        ILogger<LocalMultipartStorageService> logger)
    {
        var configuredBaseUrl = configuration["App:BaseUrl"]
            ?? configuration["ASPNETCORE_URLS"]?.Split(';')[0]
            ?? "http://localhost:5035";

        _baseUrl = configuredBaseUrl.TrimEnd('/');
        _localStoragePathProvider = localStoragePathProvider;
        _logger = logger;
    }

    public Task<InitiateMultipartResult> InitiateMultipartUploadAsync(
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new BadRequestException("Storage key is required.");
        }

        var uploadId = Guid.NewGuid().ToString("N");
        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var stagingDirectory = Path.Combine(basePath, ".parts", uploadId);

        Directory.CreateDirectory(stagingDirectory);

        _logger.LogInformation("Initiated local multipart upload {UploadId} for storage key {StorageKey}", uploadId, storageKey);

        var result = new InitiateMultipartResult(uploadId, storageKey, DefaultPartSizeBytes);
        return Task.FromResult(result);
    }

    public Task<string> CreatePartUploadUrlAsync(
        string storageKey,
        string uploadId,
        int partNumber,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var partUploadUrl = $"{_baseUrl}/api/v1/files/multipart/part?key={Uri.EscapeDataString(storageKey)}&uploadId={Uri.EscapeDataString(uploadId)}&partNumber={partNumber}";
        return Task.FromResult(partUploadUrl);
    }

    public Task<IReadOnlyList<UploadedPartDto>> ListUploadedPartsAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uploadId) ||
            uploadId.Contains("..") ||
            Path.GetFileName(uploadId) != uploadId)
        {
            throw new BadRequestException("Invalid upload ID.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var stagingDirectory = Path.Combine(basePath, ".parts", uploadId);

        if (!Directory.Exists(stagingDirectory))
        {
            return Task.FromResult<IReadOnlyList<UploadedPartDto>>(Array.Empty<UploadedPartDto>());
        }

        var partFiles = Directory.GetFiles(stagingDirectory, "part-*.tmp");
        var uploadedParts = new List<UploadedPartDto>();

        foreach (var partFilePath in partFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(partFilePath);
            var partNumberString = fileName.Replace("part-", string.Empty);

            if (int.TryParse(partNumberString, out var partNumber))
            {
                var fileInfo = new FileInfo(partFilePath);
                uploadedParts.Add(new UploadedPartDto(partNumber, $"\"part-{partNumber}\"", fileInfo.Length));
            }
        }

        IReadOnlyList<UploadedPartDto> sortedParts = uploadedParts.OrderBy(part => part.PartNumber).ToList();
        return Task.FromResult(sortedParts);
    }

    public async Task CompleteMultipartUploadAsync(
        string storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> parts,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new BadRequestException("Storage key is required.");
        }

        if (string.IsNullOrWhiteSpace(uploadId) ||
            uploadId.Contains("..") ||
            Path.GetFileName(uploadId) != uploadId)
        {
            throw new BadRequestException("Invalid upload ID.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var normalizedBasePath = basePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var destinationFullPath = Path.GetFullPath(Path.Combine(basePath, storageKey));

        if (!destinationFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase) &&
            !destinationFullPath.Equals(basePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid storage key path.");
        }

        var stagingDirectory = Path.Combine(basePath, ".parts", uploadId);
        if (!Directory.Exists(stagingDirectory))
        {
            throw new NotFoundException("Multipart upload staging session not found.");
        }

        var destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        IReadOnlyList<int> partNumbers;
        if (parts != null && parts.Count > 0)
        {
            partNumbers = parts.OrderBy(part => part.PartNumber).Select(part => part.PartNumber).ToList();
        }
        else
        {
            var existingFiles = Directory.GetFiles(stagingDirectory, "part-*.tmp");
            var extractedNumbers = new List<int>();
            foreach (var filePath in existingFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var numberString = fileName.Replace("part-", string.Empty);
                if (int.TryParse(numberString, out var number))
                {
                    extractedNumbers.Add(number);
                }
            }
            partNumbers = extractedNumbers.OrderBy(number => number).ToList();
        }

        if (partNumbers.Count == 0)
        {
            throw new BadRequestException("No parts to complete multipart upload.");
        }

        await using (var destinationStream = new FileStream(destinationFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            foreach (var partNumber in partNumbers)
            {
                var partFilePath = Path.Combine(stagingDirectory, $"part-{partNumber}.tmp");
                if (!File.Exists(partFilePath))
                {
                    throw new NotFoundException($"Part {partNumber} not found in staging area.");
                }

                await using (var partStream = new FileStream(partFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await partStream.CopyToAsync(destinationStream, cancellationToken);
                }
            }
        }

        Directory.Delete(stagingDirectory, recursive: true);
        _logger.LogInformation("Completed local multipart upload {UploadId} into {StorageKey}", uploadId, storageKey);
    }

    public Task AbortMultipartUploadAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uploadId) ||
            uploadId.Contains("..") ||
            Path.GetFileName(uploadId) != uploadId)
        {
            throw new BadRequestException("Invalid upload ID.");
        }

        var basePath = Path.GetFullPath(_localStoragePathProvider.GetBasePath());
        var stagingDirectory = Path.Combine(basePath, ".parts", uploadId);

        if (Directory.Exists(stagingDirectory))
        {
            Directory.Delete(stagingDirectory, recursive: true);
            _logger.LogInformation("Aborted local multipart upload {UploadId}", uploadId);
        }

        return Task.CompletedTask;
    }
}
