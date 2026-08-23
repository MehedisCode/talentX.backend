using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Resumes.Commands.UploadResume;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class UploadResumeCommandHandlerTests
{
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IResumeExtractionService> _resumeExtractionServiceMock;
    private readonly Mock<ILogger<UploadResumeCommandHandler>> _loggerMock;
    private readonly UploadResumeCommandHandler _handler;

    public UploadResumeCommandHandlerTests()
    {
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _resumeExtractionServiceMock = new Mock<IResumeExtractionService>();
        _loggerMock = new Mock<ILogger<UploadResumeCommandHandler>>();

        _handler = new UploadResumeCommandHandler(
            _fileStorageServiceMock.Object,
            _resumeExtractionServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSaveFile_AndReturnExtractionResult_WhenUploadAndExtractionSucceed()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new UploadResumeCommand(fileMock.Object);
        var expectedStorageKey = "resumes/1234_resume.pdf";
        var expectedExtractionResult = new ResumeExtractionResult
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Phone = "+1234567890",
            Location = "San Francisco, CA",
            LinkedInUrl = "https://linkedin.com/in/johndoe",
            TechnicalSkills = new List<string> { "C#", ".NET" }
        };

        _fileStorageServiceMock
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStorageKey);

        _resumeExtractionServiceMock
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedExtractionResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.FullName.Should().Be("John Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.FileStorageKey.Should().Be(expectedStorageKey);
        result.ResumeUploadId.Should().NotBeEmpty();

        _fileStorageServiceMock.Verify(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _resumeExtractionServiceMock.Verify(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowBadGatewayException_WhenStorageServiceFails()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new UploadResumeCommand(fileMock.Object);

        _fileStorageServiceMock
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("S3 Connection failed"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("File storage is temporarily unavailable.");

        _resumeExtractionServiceMock.Verify(e => e.ExtractAsync(It.IsAny<IUploadedFile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDeleteStoredFile_AndThrowBadGatewayException_WhenExtractionFails()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");

        var command = new UploadResumeCommand(fileMock.Object);
        var storageKey = "resumes/1234_resume.pdf";

        _fileStorageServiceMock
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);

        _resumeExtractionServiceMock
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Gemini API timeout"));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadGatewayException>()
            .WithMessage("AI extraction service is temporarily unavailable.");

        _fileStorageServiceMock.Verify(s => s.DeleteAsync(storageKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRethrowUnprocessableEntityException_WhenExtractionServiceThrowsUnprocessableEntityException()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("scanned.pdf");

        var command = new UploadResumeCommand(fileMock.Object);
        var storageKey = "resumes/1234_scanned.pdf";

        _fileStorageServiceMock
            .Setup(s => s.SaveAsync(fileMock.Object, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storageKey);

        _resumeExtractionServiceMock
            .Setup(e => e.ExtractAsync(fileMock.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnprocessableEntityException("No extractable text found in the uploaded PDF file."));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnprocessableEntityException>()
            .WithMessage("No extractable text found in the uploaded PDF file.");

        _fileStorageServiceMock.Verify(s => s.DeleteAsync(storageKey, It.IsAny<CancellationToken>()), Times.Once);
    }
}
