using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.ReplaceResume;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ReplaceResume;

public class ReplaceResumeCommandHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly Mock<IResumeExtractionService> _resumeExtractionService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<ReplaceResumeCommandHandler>> _logger = new();
    private readonly ReplaceResumeCommandHandler _handler;

    public ReplaceResumeCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");

        _handler = new ReplaceResumeCommandHandler(
            _fileStorageService.Object,
            _resumeExtractionService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException_AndTouchesNoStorage()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var act = () => _handler.Handle(new ReplaceResumeCommand(fileMock.Object), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _fileStorageService.Verify(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _resumeExtractionService.Verify(e => e.ExtractAsync(It.IsAny<IUploadedFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidPdf_ReturnsExtractionPreviewAndTouchesNoRepository()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);
        var storageKey = "resumes/user-1/1234_resume.pdf";
        var extractionResult = new ResumeExtractionResult { FullName = "Jane Doe" };

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractionResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Jane Doe");
        result.FileStorageKey.Should().Be(storageKey);
        result.ResumeUploadId.Should().NotBeEmpty();
        _fileStorageService.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BuildsStorageKeyUnderResumesPrefix_ScopedToUser()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);
        string? capturedStorageFileName = null;

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IUploadedFile, string, CancellationToken>((_, storageFileName, _) => capturedStorageFileName = storageFileName)
            .ReturnsAsync("resumes/user-1/1234_resume.pdf");
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumeExtractionResult());

        await _handler.Handle(command, CancellationToken.None);

        capturedStorageFileName.Should().StartWith("resumes/user-1/");
        capturedStorageFileName.Should().EndWith("_resume.pdf");
    }

    [Fact]
    public async Task Handle_SanitizesFileName_BeforeItReachesStorage()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("../../etc/passwd/resume.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);
        string? capturedStorageFileName = null;

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IUploadedFile, string, CancellationToken>((_, storageFileName, _) => capturedStorageFileName = storageFileName)
            .ReturnsAsync("resumes/user-1/1234_resume.pdf");
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumeExtractionResult());

        await _handler.Handle(command, CancellationToken.None);

        capturedStorageFileName.Should().NotBeNull();
        capturedStorageFileName.Should().NotContain("..");
        capturedStorageFileName.Should().EndWith("_resume.pdf");
    }

    [Fact]
    public async Task Handle_WhenUserIdContainsSlash_ThrowsBadRequestException_AndTouchesNoStorage()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("../evil-user");
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _fileStorageService.Verify(s => s.SaveAsync(It.IsAny<IUploadedFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _resumeExtractionService.Verify(e => e.ExtractAsync(It.IsAny<IUploadedFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOriginalFileNameContainsBackslash_SanitizesFileName_DeterministicallyAcrossOperatingSystems()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("notes\\v2.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);
        string? capturedStorageFileName = null;

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IUploadedFile, string, CancellationToken>((_, storageFileName, _) => capturedStorageFileName = storageFileName)
            .ReturnsAsync("resumes/user-1/1234_v2.pdf");
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumeExtractionResult());

        await _handler.Handle(command, CancellationToken.None);

        capturedStorageFileName.Should().NotBeNull();
        capturedStorageFileName.Should().NotContain("\\");
        capturedStorageFileName.Should().EndWith("_v2.pdf");
    }

    [Fact]
    public async Task Handle_WhenExtractionFails_DeletesStoredFile_AndThrowsBadGatewayException()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);
        var storageKey = "resumes/user-1/1234_resume.pdf";

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Gemini API timeout"));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
        _fileStorageService.Verify(s => s.DeleteAsync(storageKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExtractionThrowsUnprocessableEntityException_Rethrows()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("scanned.pdf");

        var command = new ReplaceResumeCommand(fileMock.Object);

        _fileStorageService
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("resumes/user-1/1234_scanned.pdf");
        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableEntityException("No extractable text found."));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }
}
