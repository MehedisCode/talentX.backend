using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.FileStorage;
using Xunit;

namespace TalentX.Infrastructure.Tests.FileStorage;

public sealed class S3MultipartStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly AwsS3Settings _settings;
    private readonly Mock<ILogger<S3MultipartStorageService>> _loggerMock;
    private readonly S3MultipartStorageService _service;

    public S3MultipartStorageServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _settings = new AwsS3Settings
        {
            BucketName = "talentx-test-bucket",
            Region = "ap-southeast-1",
            FolderPrefix = "resumes"
        };
        _loggerMock = new Mock<ILogger<S3MultipartStorageService>>();

        var optionsWrapper = Options.Create(_settings);
        _service = new S3MultipartStorageService(
            _s3ClientMock.Object,
            optionsWrapper,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InitiateMultipartUploadAsync_CallsS3AndReturnsResult()
    {
        var storageKey = "videos/presentation.mp4";
        var contentType = "video/mp4";
        var expectedUploadId = "s3-upload-id-999";

        InitiateMultipartUploadRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<InitiateMultipartUploadRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new InitiateMultipartUploadResponse
            {
                UploadId = expectedUploadId
            });

        var result = await _service.InitiateMultipartUploadAsync(storageKey, contentType);

        result.Should().NotBeNull();
        result.UploadId.Should().Be(expectedUploadId);
        result.StorageKey.Should().Be(storageKey);
        result.PartSizeBytes.Should().Be(5 * 1024 * 1024);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.ContentType.Should().Be(contentType);
    }

    [Fact]
    public async Task InitiateMultipartUploadAsync_ThrowsBadGatewayException_OnS3Exception()
    {
        _s3ClientMock
            .Setup(client => client.InitiateMultipartUploadAsync(It.IsAny<InitiateMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Service Unavailable") { StatusCode = HttpStatusCode.ServiceUnavailable });

        var act = () => _service.InitiateMultipartUploadAsync("video.mp4", "video/mp4");

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("File storage is temporarily unavailable.");
    }

    [Fact]
    public async Task CreatePartUploadUrlAsync_GeneratesPresignedPutUrlWithUploadIdAndPartNumber()
    {
        var storageKey = "videos/presentation.mp4";
        var uploadId = "upload-part-test";
        var partNumber = 3;
        var expiry = TimeSpan.FromMinutes(10);
        var expectedUrl = "https://s3.amazonaws.com/talentx-test-bucket/videos/presentation.mp4?uploadId=upload-part-test&partNumber=3";

        GetPreSignedUrlRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => capturedRequest = request)
            .Returns(expectedUrl);

        var result = await _service.CreatePartUploadUrlAsync(storageKey, uploadId, partNumber, expiry);

        result.Should().Be(expectedUrl);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.UploadId.Should().Be(uploadId);
        capturedRequest.PartNumber.Should().Be(partNumber);
        capturedRequest.Verb.Should().Be(HttpVerb.PUT);
    }

    [Fact]
    public async Task ListUploadedPartsAsync_CallsS3AndReturnsUploadedParts()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-list-test";

        ListPartsRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ListPartsRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new ListPartsResponse
            {
                Parts = new List<PartDetail>
                {
                    new() { PartNumber = 2, ETag = "\"etag-2\"", Size = 5242880 },
                    new() { PartNumber = 1, ETag = "\"etag-1\"", Size = 5242880 }
                }
            });

        var parts = await _service.ListUploadedPartsAsync(storageKey, uploadId);

        parts.Should().HaveCount(2);
        parts[0].PartNumber.Should().Be(1);
        parts[0].ETag.Should().Be("\"etag-1\"");
        parts[0].SizeBytes.Should().Be(5242880);

        parts[1].PartNumber.Should().Be(2);
        parts[1].ETag.Should().Be("\"etag-2\"");
        parts[1].SizeBytes.Should().Be(5242880);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.UploadId.Should().Be(uploadId);
    }

    [Fact]
    public async Task ListUploadedPartsAsync_ThrowsBadGatewayException_OnS3Exception()
    {
        _s3ClientMock
            .Setup(client => client.ListPartsAsync(It.IsAny<ListPartsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NoSuchUpload") { StatusCode = HttpStatusCode.NotFound });

        var act = () => _service.ListUploadedPartsAsync("sample.mp4", "invalid-upload-id");

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("File storage is temporarily unavailable.");
    }

    [Fact]
    public async Task CompleteMultipartUploadAsync_CallsS3CompleteMultipartUpload()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-complete-test";
        var parts = new List<PartETagDto>
        {
            new(1, "\"etag-1\""),
            new(2, "\"etag-2\"")
        };

        CompleteMultipartUploadRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CompleteMultipartUploadRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new CompleteMultipartUploadResponse());

        await _service.CompleteMultipartUploadAsync(storageKey, uploadId, parts);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.UploadId.Should().Be(uploadId);
        capturedRequest.PartETags.Should().HaveCount(2);
        capturedRequest.PartETags[0].PartNumber.Should().Be(1);
        capturedRequest.PartETags[0].ETag.Should().Be("\"etag-1\"");
        capturedRequest.PartETags[1].PartNumber.Should().Be(2);
        capturedRequest.PartETags[1].ETag.Should().Be("\"etag-2\"");
    }

    [Fact]
    public async Task CompleteMultipartUploadAsync_ThrowsBadGatewayException_OnS3Exception()
    {
        _s3ClientMock
            .Setup(client => client.CompleteMultipartUploadAsync(It.IsAny<CompleteMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("InvalidPart") { StatusCode = HttpStatusCode.BadRequest });

        var act = () => _service.CompleteMultipartUploadAsync("sample.mp4", "upload-id", new List<PartETagDto>());

        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("File storage is temporarily unavailable.");
    }

    [Fact]
    public async Task AbortMultipartUploadAsync_CallsS3AbortMultipartUpload()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-abort-test";

        AbortMultipartUploadRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AbortMultipartUploadRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(new AbortMultipartUploadResponse());

        await _service.AbortMultipartUploadAsync(storageKey, uploadId);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.UploadId.Should().Be(uploadId);
    }

    [Fact]
    public async Task AbortMultipartUploadAsync_DoesNotThrow_OnS3Exception()
    {
        _s3ClientMock
            .Setup(client => client.AbortMultipartUploadAsync(It.IsAny<AbortMultipartUploadRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("NoSuchUpload") { StatusCode = HttpStatusCode.NotFound });

        var act = () => _service.AbortMultipartUploadAsync("sample.mp4", "upload-id");

        await act.Should().NotThrowAsync();
    }
}
