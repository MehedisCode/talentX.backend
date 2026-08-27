using System.Text;
using FluentAssertions;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Files.Queries.GetLocalFileStream;
using Xunit;

namespace TalentX.Application.Tests.Features.Files;

public sealed class GetLocalFileStreamQueryHandlerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<ILocalStoragePathProvider> _pathProviderMock;
    private readonly GetLocalFileStreamQueryHandler _handler;

    public GetLocalFileStreamQueryHandlerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TalentX_GetLocalFileStreamTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _pathProviderMock = new Mock<ILocalStoragePathProvider>();
        _pathProviderMock.Setup(provider => provider.GetBasePath()).Returns(_testDirectory);

        _handler = new GetLocalFileStreamQueryHandler(_pathProviderMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Handle_ReturnsFileStreamResult_WhenFileExists()
    {
        var relativePath = "resumes/candidate.pdf";
        var fullPath = Path.Combine(_testDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "PDF content");

        var query = new GetLocalFileStreamQuery(relativePath);
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Be("candidate.pdf");

        using var streamReader = new StreamReader(result.Stream);
        var readContent = await streamReader.ReadToEndAsync();
        readContent.Should().Be("PDF content");
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_WhenStorageKeyIsEmpty()
    {
        var query = new GetLocalFileStreamQuery(string.Empty);

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Storage key is required.");
    }

    [Fact]
    public async Task Handle_ThrowsBadRequestException_OnPathTraversal()
    {
        var query = new GetLocalFileStreamQuery("../../etc/passwd");

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid storage key path.");
    }

    [Fact]
    public async Task Handle_ThrowsNotFoundException_WhenFileDoesNotExist()
    {
        var query = new GetLocalFileStreamQuery("resumes/nonexistent.pdf");

        var act = () => _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("File not found.");
    }
}
