using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;

namespace TalentX.Infrastructure.FileStorage;

/// <summary>
/// Production file storage implementation that uploads resumes to AWS S3.
/// Returns the S3 object key (e.g. "resumes/{storageFileName}") as the
/// FileStorageKey — never a public URL, preserving the option to generate
/// pre-signed URLs on demand.
/// </summary>
public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Settings _settings;
    private readonly ILogger<S3FileStorageService> _logger;

    public S3FileStorageService(
        IAmazonS3 s3Client,
        IOptions<AwsS3Settings> settings,
        ILogger<S3FileStorageService> logger)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> SaveAsync(
        IUploadedFile file,
        string storageFileName,
        CancellationToken cancellationToken = default)
    {
        var objectKey = string.IsNullOrWhiteSpace(_settings.FolderPrefix)
            ? storageFileName
            : $"{_settings.FolderPrefix.TrimEnd('/')}/{storageFileName}";

        _logger.LogInformation(
            "Uploading file {FileName} to S3 bucket {Bucket} with key {ObjectKey}",
            file.FileName, _settings.BucketName, objectKey);

        try
        {
            var transferUtility = new TransferUtility(_s3Client);

            await using var stream = file.OpenReadStream();

            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = file.ContentType,
                // Prevent public access — access via pre-signed URLs only
                CannedACL = S3CannedACL.Private,
                AutoCloseStream = false,
            };

            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded {FileName} to S3. ObjectKey={ObjectKey}, Bucket={Bucket}",
                file.FileName, objectKey, _settings.BucketName);

            return objectKey;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex,
                "AWS S3 error while uploading {FileName}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                file.FileName, ex.StatusCode, ex.ErrorCode);

            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while uploading {FileName} to S3",
                file.FileName);

            throw new BadGatewayException("File storage is temporarily unavailable.");
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.DeleteObjectAsync(_settings.BucketName, storageKey, cancellationToken);

            _logger.LogInformation(
                "Deleted S3 object {StorageKey} from bucket {Bucket}.",
                storageKey, _settings.BucketName);
        }
        catch (AmazonS3Exception ex)
        {
            // S3 DeleteObject is idempotent — a 404 means it's already gone, which is fine.
            _logger.LogWarning(ex,
                "AWS S3 error while deleting {StorageKey}. StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                storageKey, ex.StatusCode, ex.ErrorCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Unexpected error while deleting {StorageKey} from S3",
                storageKey);
        }
    }
}
