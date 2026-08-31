using System.Text;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Files.Commands.UploadLocalFile;
using Xunit;

namespace TalentX.Application.Tests.Features.Files;

public sealed class UploadLocalFileCommandHandlerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILocalStoragePathProvider> _pathProviderMock;
    private readonly UploadLocalFileCommandHandler _handler;

    public UploadLocalFileCommandHandlerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TalentX_UploadLocalFileTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _pathProviderMock = new Mock<ILocalStoragePathProvider>();
        _pathProviderMock.Setup(provider => provider.GetBasePath()).Returns(_testDirectory);

        _handler = new UploadLocalFileCommandHandler(_pathProviderMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Handle_WritesStreamToDestination_AndReturnsStorageKey()
    {
        var relativePath = "resumes/candidate.pdf";
        var payload = "Test PDF file bytes";
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        var command = new UploadLocalFileCommand(relativePath, memoryStream);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(relativePath);

        var destinationFullPath = Path.Combine(_testDirectory, relativePath);
        File.Exists(destinationFullPath).Should().BeTrue();

        var writtenContent = await File.ReadAllTextAsync(destinationFullPath);
        writtenContent.Should().Be(payload);
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_WhenStorageKeyIsEmpty()
    {
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
        var command = new UploadLocalFileCommand(string.Empty, memoryStream);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Storage key is required.");
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_OnPathTraversal()
    {
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
        var command = new UploadLocalFileCommand("../../secret.txt", memoryStream);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid storage key path.");
    }
}
