using System.Text;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;
using Xunit;

namespace TalentX.Application.Tests.Features.Files;

public sealed class SaveLocalMultipartPartCommandHandlerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILocalStoragePathProvider> _pathProviderMock;
    private readonly SaveLocalMultipartPartCommandHandler _handler;

    public SaveLocalMultipartPartCommandHandlerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TalentX_SaveLocalPartTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _pathProviderMock = new Mock<ILocalStoragePathProvider>();
        _pathProviderMock.Setup(provider => provider.GetBasePath()).Returns(_testDirectory);

        _handler = new SaveLocalMultipartPartCommandHandler(_pathProviderMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Handle_SavesPartChunk_AndReturnsUploadedPartDto()
    {
        var storageKey = "videos/sample.mp4";
        var uploadId = "upload-session-abc";
        var partNumber = 1;
        var payload = "Chunk 1 bytes";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var command = new SaveLocalMultipartPartCommand(storageKey, uploadId, partNumber, memoryStream);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.PartNumber.Should().Be(1);
        result.ETag.Should().Be("\"part-1\"");
        result.SizeBytes.Should().Be(Encoding.UTF8.GetByteCount(payload));

        var partPath = Path.Combine(_testDirectory, ".parts", uploadId, "part-1.tmp");
        File.Exists(partPath).Should().BeTrue();

        var writtenContent = await File.ReadAllTextAsync(partPath);
        writtenContent.Should().Be(payload);
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_WhenUploadIdHasPathTraversal()
    {
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Chunk"));
        var command = new SaveLocalMultipartPartCommand("sample.mp4", "../../traversal", 1, memoryStream);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid upload ID.");
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_WhenStorageKeyHasPathTraversal()
    {
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Chunk"));
        var command = new SaveLocalMultipartPartCommand("../../etc/passwd", "valid-upload-id", 1, memoryStream);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid storage key path.");
    }
}
