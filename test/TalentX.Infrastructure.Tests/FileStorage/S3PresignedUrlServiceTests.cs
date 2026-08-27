using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TalentX.Infrastructure.FileStorage;
using Xunit;

namespace TalentX.Infrastructure.Tests.FileStorage;

public sealed class S3PresignedUrlServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly AwsS3Settings _settings;
    private readonly S3PresignedUrlService _service;

    public S3PresignedUrlServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _settings = new AwsS3Settings
        {
            BucketName = "talentx-test-bucket",
            Region = "ap-southeast-1",
            FolderPrefix = "resumes"
        };

        var optionsWrapper = Options.Create(_settings);
        _service = new S3PresignedUrlService(_s3ClientMock.Object, optionsWrapper);
    }

    [Fact]
    public async Task CreateUploadUrlAsync_GeneratesPresignedPutUrlAndTarget()
    {
        var storageKey = "resumes/candidate-resume-123.pdf";
        var contentType = "application/pdf";
        var expiry = TimeSpan.FromMinutes(15);
        var expectedUrl = "https://talentx-test-bucket.s3.ap-southeast-1.amazonaws.com/resumes/candidate-resume-123.pdf?signature=test";

        GetPreSignedUrlRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => capturedRequest = request)
            .Returns(expectedUrl);

        var startTime = DateTimeOffset.UtcNow;
        var result = await _service.CreateUploadUrlAsync(storageKey, contentType, expiry);
        var endTime = DateTimeOffset.UtcNow;

        result.Should().NotBeNull();
        result.UploadUrl.Should().Be(expectedUrl);
        result.StorageKey.Should().Be(storageKey);
        result.HttpMethod.Should().Be("PUT");
        result.ExpiresAt.Should().BeOnOrAfter(startTime.Add(expiry)).And.BeOnOrBefore(endTime.Add(expiry));

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.Verb.Should().Be(HttpVerb.PUT);
        capturedRequest.ContentType.Should().Be(contentType);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_GeneratesPresignedGetUrl()
    {
        var storageKey = "resumes/candidate-resume-123.pdf";
        var expiry = TimeSpan.FromHours(1);
        var expectedUrl = "https://talentx-test-bucket.s3.ap-southeast-1.amazonaws.com/resumes/candidate-resume-123.pdf?signature=download";

        GetPreSignedUrlRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(client => client.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Callback<GetPreSignedUrlRequest>(request => capturedRequest = request)
            .Returns(expectedUrl);

        var result = await _service.CreateDownloadUrlAsync(storageKey, expiry);

        result.Should().Be(expectedUrl);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be("talentx-test-bucket");
        capturedRequest.Key.Should().Be(storageKey);
        capturedRequest.Verb.Should().Be(HttpVerb.GET);
    }
}
