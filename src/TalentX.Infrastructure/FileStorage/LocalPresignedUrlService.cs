using Microsoft.Extensions.Configuration;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.FileStorage;

public sealed class LocalPresignedUrlService : IPresignedUrlService
{
    private readonly string _baseUrl;

    public LocalPresignedUrlService(IConfiguration configuration)
    {
        var configuredBaseUrl = configuration["App:BaseUrl"]
            ?? configuration["ASPNETCORE_URLS"]?.Split(';')[0]
            ?? "http://localhost:5035";

        _baseUrl = configuredBaseUrl.TrimEnd('/');
    }

    public Task<PresignedUploadTarget> CreateUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry);
        var uploadUrl = $"{_baseUrl}/api/v1/files/upload?key={Uri.EscapeDataString(storageKey)}";

        var uploadTarget = new PresignedUploadTarget(
            UploadUrl: uploadUrl,
            StorageKey: storageKey,
            ExpiresAt: expiresAt,
            HttpMethod: "PUT");

        return Task.FromResult(uploadTarget);
    }

    public Task<string> CreateDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var downloadUrl = $"{_baseUrl}/api/v1/files/stream?key={Uri.EscapeDataString(storageKey)}";

        return Task.FromResult(downloadUrl);
    }
}
