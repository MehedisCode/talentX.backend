using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Talents.Commands.ExtractResume;
using TalentX.Application.Identity.Services;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ExtractResume;

// ExtractResumeCommandHandler no longer takes an IFileStorageService dependency at all
// (dropped entirely by the Phase 4B redesign), so "extraction never touches storage" is
// proven at compile time by this constructor signature, not by a runtime mock verification.
public class ExtractResumeCommandHandlerTests
{
    private readonly Mock<IResumeExtractionService> _resumeExtractionService = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<ExtractResumeCommandHandler>> _logger = new();
    private readonly ExtractResumeCommandHandler _handler;

    public ExtractResumeCommandHandlerTests()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns("user-1");

        _handler = new ExtractResumeCommandHandler(
            _resumeExtractionService.Object,
            _currentUserService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotAuthenticated_ThrowsUnauthorizedException_AndTouchesNoExtractionOrStorage()
    {
        _currentUserService.SetupGet(u => u.UserId).Returns((string?)null);
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var act = () => _handler.Handle(new ExtractResumeCommand(fileMock.Object), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _resumeExtractionService.Verify(e => e.ExtractAsync(It.IsAny<IUploadedFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidPdf_ReturnsExtractionPreview_AndNeverTouchesStorage()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ExtractResumeCommand(fileMock.Object);
        var extractionResult = new ResumeExtractionResult { FullName = "Jane Doe" };

        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractionResult);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Jane Doe");
        result.ResumeUploadId.Should().NotBeEmpty();
        result.FileStorageKey.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenExtractionThrowsUnprocessableEntityException_Rethrows_AndTouchesNoStorage()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("scanned.pdf");

        var command = new ExtractResumeCommand(fileMock.Object);

        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableEntityException("No extractable text found."));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnprocessableEntityException>();
    }

    [Fact]
    public async Task Handle_WhenExtractionFailsForOtherReason_ThrowsBadGatewayException_AndTouchesNoStorage()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new ExtractResumeCommand(fileMock.Object);

        _resumeExtractionService
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Gemini API timeout"));

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadGatewayException>();
    }
}
