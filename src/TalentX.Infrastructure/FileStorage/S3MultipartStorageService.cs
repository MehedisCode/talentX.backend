using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;

namespace TalentX.Infrastructure.FileStorage;

public sealed class S3MultipartStorageService : IMultipartStorageService
{
    private const int DefaultPartSizeBytes = 5 * 1024 * 1024;
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Settings _settings;
    private readonly ILogger<S3MultipartStorageService> _logger;

    public S3MultipartStorageService(
        IAmazonS3 s3Client,
        IOptions<AwsS3Settings> settings,
        ILogger<S3MultipartStorageService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<InitiateMultipartResult> InitiateMultipartUploadAsync(
        string storageKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var initiateRequest = new InitiateMultipartUploadRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            ContentType = contentType
        };

        try
        {
            var response = await _s3Client.InitiateMultipartUploadAsync(initiateRequest, cancellationToken);
            _logger.LogInformation("Initiated S3 multipart upload {UploadId} for key {StorageKey}", response.UploadId, storageKey);
            return new InitiateMultipartResult(response.UploadId, storageKey, DefaultPartSizeBytes);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogError(
                exception,
                "AWS S3 error while initiating multipart upload for {StorageKey}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                storageKey,
                exception.StatusCode,
                exception.ErrorCode);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while initiating S3 multipart upload for {StorageKey}", storageKey);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
    }

    public Task<string> CreatePartUploadUrlAsync(
        string storageKey,
        string uploadId,
        int partNumber,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var presignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            UploadId = uploadId,
            PartNumber = partNumber,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var uploadUrl = _s3Client.GetPreSignedURL(presignedRequest);
        return Task.FromResult(uploadUrl);
    }

    public async Task<IReadOnlyList<UploadedPartDto>> ListUploadedPartsAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var listPartsRequest = new ListPartsRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            UploadId = uploadId
        };

        try
        {
            var response = await _s3Client.ListPartsAsync(listPartsRequest, cancellationToken);
            var uploadedParts = response.Parts
                .Select(part => new UploadedPartDto(part.PartNumber, part.ETag, part.Size))
                .OrderBy(part => part.PartNumber)
                .ToList();

            return uploadedParts;
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogError(
                exception,
                "AWS S3 error while listing parts for upload {UploadId} and key {StorageKey}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                uploadId,
                storageKey,
                exception.StatusCode,
                exception.ErrorCode);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while listing S3 parts for upload {UploadId} and key {StorageKey}", uploadId, storageKey);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
    }

    public async Task CompleteMultipartUploadAsync(
        string storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> parts,
        CancellationToken cancellationToken = default)
    {
        var completeRequest = new CompleteMultipartUploadRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            UploadId = uploadId,
            PartETags = parts.Select(part => new PartETag
            {
                PartNumber = part.PartNumber,
                ETag = part.ETag
            }).ToList()
        };

        try
        {
            await _s3Client.CompleteMultipartUploadAsync(completeRequest, cancellationToken);
            _logger.LogInformation("Completed S3 multipart upload {UploadId} for key {StorageKey}", uploadId, storageKey);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogError(
                exception,
                "AWS S3 error while completing multipart upload {UploadId} for key {StorageKey}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                uploadId,
                storageKey,
                exception.StatusCode,
                exception.ErrorCode);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unexpected error while completing S3 multipart upload {UploadId} for key {StorageKey}", uploadId, storageKey);
            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
    }

    public async Task AbortMultipartUploadAsync(
        string storageKey,
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        var abortRequest = new AbortMultipartUploadRequest
        {
            BucketName = _settings.BucketName,
            Key = storageKey,
            UploadId = uploadId
        };

        try
        {
            await _s3Client.AbortMultipartUploadAsync(abortRequest, cancellationToken);
            _logger.LogInformation("Aborted S3 multipart upload {UploadId} for key {StorageKey}", uploadId, storageKey);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogWarning(
                exception,
                "AWS S3 error while aborting multipart upload {UploadId} for key {StorageKey}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                uploadId,
                storageKey,
                exception.StatusCode,
                exception.ErrorCode);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unexpected error while aborting S3 multipart upload {UploadId} for key {StorageKey}", uploadId, storageKey);
        }
    }
}
