using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Infrastructure.FileStorage;
using Xunit;

namespace TalentX.Infrastructure.Tests.FileStorage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalStoragePathProvider _pathProvider;
    private readonly Mock<ILogger<LocalFileStorageService>> _loggerMock;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "TalentX_LocalFileStorageServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);

        _pathProvider = new LocalStoragePathProvider(_testDirectory);
        _loggerMock = new Mock<ILogger<LocalFileStorageService>>();

        _service = new LocalFileStorageService(
            _pathProvider,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task SaveAsync_WithNestedDirectories_CreatesDirectoriesAndSavesFile()
    {
        var fileContent = "PDF content sample";
        var uploadedFileMock = new Mock<IUploadedFile>();
        uploadedFileMock.Setup(f => f.OpenReadStream())
            .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(fileContent)));

        var storageFileName = "resumes/user-123/550e8400_sample_resume.pdf";

        var returnedKey = await _service.SaveAsync(uploadedFileMock.Object, storageFileName);

        returnedKey.Should().Be(storageFileName);
        var expectedFilePath = Path.Combine(_testDirectory, storageFileName);
        File.Exists(expectedFilePath).Should().BeTrue();
        (await File.ReadAllTextAsync(expectedFilePath)).Should().Be(fileContent);
    }

    [Fact]
    public async Task SaveAsync_WithPathTraversal_ThrowsBadRequestException()
    {
        var uploadedFileMock = new Mock<IUploadedFile>();
        uploadedFileMock.Setup(f => f.OpenReadStream())
            .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("malicious content")));

        var act = () => _service.SaveAsync(uploadedFileMock.Object, "../../outside_file.txt");

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesFile()
    {
        var relativePath = "resumes/user-123/to_delete.pdf";
        var fullPath = Path.Combine(_testDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "to be deleted");

        await _service.DeleteAsync(relativePath);

        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_CompletesWithoutThrowing()
    {
        var act = () => _service.DeleteAsync("resumes/non_existent.pdf");

        await act.Should().NotThrowAsync();
    }
}
