using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.Commands.UploadResume;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes;

public class UploadResumeCommandValidatorTests
{
    private readonly UploadResumeCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFileIsNull()
    {
        // Arrange
        var command = new UploadResumeCommand(null!);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "File" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6 MB
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var command = new UploadResumeCommand(fileMock.Object);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("exceed 5 MB"));
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenExtensionIsNotSupported()
    {
        // Arrange
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.FileName).Returns("resume.txt");
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        var command = new UploadResumeCommand(fileMock.Object);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PDF and DOCX"));
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenValidPdfWithMagicBytes()
    {
        // Arrange
        var pdfHeader = "%PDF-1.4 header bytes"u8.ToArray();
        var stream = new MemoryStream(pdfHeader);

        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(pdfHeader.Length);
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var command = new UploadResumeCommand(fileMock.Object);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenValidDocxWithMagicBytes()
    {
        // Arrange
        var docxHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };
        var stream = new MemoryStream(docxHeader);

        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(docxHeader.Length);
        fileMock.Setup(f => f.FileName).Returns("resume.docx");
        fileMock.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var command = new UploadResumeCommand(fileMock.Object);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMagicBytesDoNotMatchExtension()
    {
        // Arrange
        var fakeHeader = "INVALID_HEADER_BYTES"u8.ToArray();
        var stream = new MemoryStream(fakeHeader);

        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(fakeHeader.Length);
        fileMock.Setup(f => f.FileName).Returns("fake.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var command = new UploadResumeCommand(fileMock.Object);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not match its declared type"));
    }
}
