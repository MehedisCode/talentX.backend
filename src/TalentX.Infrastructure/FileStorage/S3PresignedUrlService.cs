using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.FileStorage;

public sealed class S3PresignedUrlService : IPresignedUrlService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Settings _settings;

    public S3PresignedUrlService(
        IAmazonS3 s3Client,
        IOptions<AwsS3Settings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public Task<PresignedUploadTarget> CreateUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(expiry);

        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime
        };

        var uploadUrl = _s3Client.GetPreSignedURL(presignedRequest);

        var uploadTarget = new PresignedUploadTarget(
            UploadUrl: uploadUrl,
            StorageKey: storageKey,
            ExpiresAt: expiresAt,
            HttpMethod: "PUT");

        return Task.FromResult(uploadTarget);
    }

    public async Task<string> CreateDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var resolvedKey = await ResolveS3ObjectKeyAsync(storageKey, cancellationToken);

        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = resolvedKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return _s3Client.GetPreSignedURL(presignedRequest);
    }

    private async Task<string> ResolveS3ObjectKeyAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return storageKey;

        if (storageKey.StartsWith("job-applications/", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_settings.FolderPrefix) &&
            !storageKey.StartsWith(_settings.FolderPrefix.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        {
            var prefixedKey = $"{_settings.FolderPrefix.TrimEnd('/')}/{storageKey}";
            try
            {
                await _s3Client.GetObjectMetadataAsync(_settings.BucketName, storageKey, cancellationToken);
                return storageKey;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchKey")
            {
                try
                {
                    await _s3Client.GetObjectMetadataAsync(_settings.BucketName, prefixedKey, cancellationToken);
                    return prefixedKey;
                }
                catch (AmazonS3Exception)
                {
                    return storageKey;
                }
            }
            catch (Exception)
            {
                return storageKey;
            }
        }

        return storageKey;
    }
}
