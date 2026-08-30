using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Models;
using TalentX.Infrastructure.FileStorage;
using Xunit;

namespace TalentX.Infrastructure.Tests.FileStorage;

public sealed class LocalMultipartStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalStoragePathProvider _pathProvider;
    private readonly Mock<ILogger<LocalMultipartStorageService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly LocalMultipartStorageService _service;

    public LocalMultipartStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TalentX_LocalMultipartStorageServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "App:BaseUrl", "https://api.talentx.local:5001" },
            { "FILE_STORAGE_LOCAL_BASE_PATH", _testDirectory }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _pathProvider = new LocalStoragePathProvider(_testDirectory);
        _loggerMock = new Mock<ILogger<LocalMultipartStorageService>>();

        _service = new LocalMultipartStorageService(
            _configuration,
            _pathProvider,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task InitiateMultipartUploadAsync_CreatesStagingDirectoryAndReturnsResult()
    {
        var storageKey = "videos/interview-sample.mp4";
        var contentType = "video/mp4";

        var result = await _service.InitiateMultipartUploadAsync(storageKey, contentType);

        result.Should().NotBeNull();
        result.StorageKey.Should().Be(storageKey);
        result.UploadId.Should().NotBeNullOrWhiteSpace();
        result.PartSizeBytes.Should().Be(5 * 1024 * 1024);

        var stagingPath = Path.Combine(_testDirectory, ".parts", result.UploadId);
        Directory.Exists(stagingPath).Should().BeTrue();
    }

    [Fact]
    public async Task InitiateMultipartUploadAsync_ThrowsBadRequestException_WhenStorageKeyIsEmpty()
    {
        var act = () => _service.InitiateMultipartUploadAsync(string.Empty, "video/mp4");

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Storage key is required.");
    }

    [Fact]
    public async Task CreatePartUploadUrlAsync_GeneratesCorrectUrl()
    {
        var storageKey = "resumes/candidate portfolio.pdf";
        var uploadId = "upload-session-123";
        var partNumber = 2;
        var expiry = TimeSpan.FromMinutes(15);

        var result = await _service.CreatePartUploadUrlAsync(storageKey, uploadId, partNumber, expiry);

        result.Should().Be("https://api.talentx.local:5001/api/v1/files/multipart/part?key=resumes%2Fcandidate%20portfolio.pdf&uploadId=upload-session-123&partNumber=2");
    }

    [Fact]
    public async Task ListUploadedPartsAsync_ReturnsPartsInSortedOrder()
    {
        var uploadId = "session-list-test";
        var stagingPath = Path.Combine(_testDirectory, ".parts", uploadId);
        Directory.CreateDirectory(stagingPath);

        var part2Path = Path.Combine(stagingPath, "part-2.tmp");
        var part1Path = Path.Combine(stagingPath, "part-1.tmp");
        var part3Path = Path.Combine(stagingPath, "part-3.tmp");

        await File.WriteAllBytesAsync(part2Path, Encoding.UTF8.GetBytes("Part 2 Content"));
        await File.WriteAllBytesAsync(part1Path, Encoding.UTF8.GetBytes("Part 1"));
        await File.WriteAllBytesAsync(part3Path, Encoding.UTF8.GetBytes("Part 3 Longer Content"));

        var parts = await _service.ListUploadedPartsAsync("sample.mp4", uploadId);

        parts.Should().HaveCount(3);
        parts[0].PartNumber.Should().Be(1);
        parts[0].ETag.Should().Be("\"part-1\"");
        parts[0].SizeBytes.Should().Be(6);

        parts[1].PartNumber.Should().Be(2);
        parts[1].ETag.Should().Be("\"part-2\"");
        parts[1].SizeBytes.Should().Be(14);

        parts[2].PartNumber.Should().Be(3);
        parts[2].ETag.Should().Be("\"part-3\"");
        parts[2].SizeBytes.Should().Be(21);
    }

    [Fact]
    public async Task ListUploadedPartsAsync_ReturnsEmptyList_WhenStagingDirectoryDoesNotExist()
    {
        var parts = await _service.ListUploadedPartsAsync("sample.mp4", "nonexistent-session");

        parts.Should().BeEmpty();
    }

    [Fact]
    public async Task ListUploadedPartsAsync_ThrowsBadRequestException_WhenUploadIdHasPathTraversal()
    {
        var act = () => _service.ListUploadedPartsAsync("sample.mp4", "../traversal");

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid upload ID.");
    }

    [Fact]
    public async Task CompleteMultipartUploadAsync_AssemblesPartsAndDeletesStagingDirectory()
    {
        var storageKey = "videos/subfolder/final-video.mp4";
        var uploadId = "complete-session-123";
        var stagingPath = Path.Combine(_testDirectory, ".parts", uploadId);
        Directory.CreateDirectory(stagingPath);

        var part1Path = Path.Combine(stagingPath, "part-1.tmp");
        var part2Path = Path.Combine(stagingPath, "part-2.tmp");

        await File.WriteAllBytesAsync(part1Path, Encoding.UTF8.GetBytes("Hello, "));
        await File.WriteAllBytesAsync(part2Path, Encoding.UTF8.GetBytes("World!"));

        var parts = new List<PartETagDto>
        {
            new(2, "\"part-2\""),
            new(1, "\"part-1\"")
        };

        await _service.CompleteMultipartUploadAsync(storageKey, uploadId, parts);

        var destinationFullPath = Path.Combine(_testDirectory, storageKey);
        File.Exists(destinationFullPath).Should().BeTrue();

        var assembledContent = await File.ReadAllTextAsync(destinationFullPath);
        assembledContent.Should().Be("Hello, World!");

        Directory.Exists(stagingPath).Should().BeFalse();
    }

    [Fact]
    public async Task CompleteMultipartUploadAsync_ThrowsBadRequestException_OnPathTraversal()
    {
        var uploadId = "traversal-session";
        var stagingPath = Path.Combine(_testDirectory, ".parts", uploadId);
        Directory.CreateDirectory(stagingPath);

        var part1Path = Path.Combine(stagingPath, "part-1.tmp");
        await File.WriteAllBytesAsync(part1Path, Encoding.UTF8.GetBytes("Content"));

        var act = () => _service.CompleteMultipartUploadAsync("../../etc/passwd", uploadId, new List<PartETagDto> { new(1, "etag") });

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid storage key path.");
    }

    [Fact]
    public async Task CompleteMultipartUploadAsync_ThrowsNotFoundException_WhenStagingDirectoryDoesNotExist()
    {
        var act = () => _service.CompleteMultipartUploadAsync("destination.mp4", "missing-session", new List<PartETagDto>());

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Multipart upload staging session not found.");
    }

    [Fact]
    public async Task AbortMultipartUploadAsync_DeletesStagingDirectory()
    {
        var uploadId = "abort-session-456";
        var stagingPath = Path.Combine(_testDirectory, ".parts", uploadId);
        Directory.CreateDirectory(stagingPath);

        var part1Path = Path.Combine(stagingPath, "part-1.tmp");
        await File.WriteAllBytesAsync(part1Path, Encoding.UTF8.GetBytes("Partial upload content"));

        await _service.AbortMultipartUploadAsync("sample.mp4", uploadId);

        Directory.Exists(stagingPath).Should().BeFalse();
    }

    [Fact]
    public async Task AbortMultipartUploadAsync_ThrowsBadRequestException_WhenUploadIdHasPathTraversal()
    {
        var act = () => _service.AbortMultipartUploadAsync("sample.mp4", "../../escaped");

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid upload ID.");
    }
}
