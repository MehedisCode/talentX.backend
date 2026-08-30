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

    public Task<string> CreateDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var downloadUrl = _s3Client.GetPreSignedURL(presignedRequest);

        return Task.FromResult(downloadUrl);
    }
}
