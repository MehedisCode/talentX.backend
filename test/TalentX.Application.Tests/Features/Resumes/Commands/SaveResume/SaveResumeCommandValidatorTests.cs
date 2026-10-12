using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Resumes.Commands.SaveResume;
using TalentX.Domain.Entities;
using Xunit;

namespace TalentX.Application.Tests.Features.Resumes.Commands.SaveResume;

public sealed class SaveResumeCommandValidatorTests
{
    private readonly SaveResumeCommandValidator _validator = new();

    private static Mock<IUploadedFile> CreateValidPdfFileMock()
    {
        var pdfHeader = "%PDF-1.4 header bytes"u8.ToArray();
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(file => file.Length).Returns(pdfHeader.Length);
        fileMock.Setup(file => file.FileName).Returns("resume.pdf");
        fileMock.Setup(file => file.ContentType).Returns("application/pdf");
        fileMock.Setup(file => file.OpenReadStream()).Returns(() => new MemoryStream(pdfHeader));
        return fileMock;
    }

    private static Mock<IUploadedFile> CreateValidDocxFileMock()
    {
        var docxHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(file => file.Length).Returns(docxHeader.Length);
        fileMock.Setup(file => file.FileName).Returns("resume.docx");
        fileMock.Setup(file => file.ContentType).Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        fileMock.Setup(file => file.OpenReadStream()).Returns(() => new MemoryStream(docxHeader));
        return fileMock;
    }

    [Fact]
    public async Task Validate_WhenValidPdfCommand_PassesValidation()
    {
        var command = new SaveResumeCommand(CreateValidPdfFileMock().Object, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenValidDocxCommand_PassesValidation()
    {
        var command = new SaveResumeCommand(CreateValidDocxFileMock().Object, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenResumeFileIsNull_FailsValidation()
    {
        var command = new SaveResumeCommand(null!, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error => error.ErrorMessage.Contains("A resume file is required"));
    }

    [Fact]
    public async Task Validate_WhenFileSizeExceeds5Mb_FailsValidation()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(file => file.Length).Returns(6 * 1024 * 1024);
        fileMock.Setup(file => file.FileName).Returns("resume.pdf");
        fileMock.Setup(file => file.ContentType).Returns("application/pdf");

        var command = new SaveResumeCommand(fileMock.Object, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error => error.ErrorMessage.Contains("File size must not exceed 5 MB"));
    }

    [Fact]
    public async Task Validate_WhenFileExtensionIsNotSupported_FailsValidation()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(file => file.Length).Returns(1024);
        fileMock.Setup(file => file.FileName).Returns("resume.txt");
        fileMock.Setup(file => file.ContentType).Returns("text/plain");

        var command = new SaveResumeCommand(fileMock.Object, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error => error.ErrorMessage.Contains("Only PDF and DOCX files are accepted"));
    }

    [Fact]
    public async Task Validate_WhenFileMagicBytesDoNotMatch_FailsValidation()
    {
        var invalidHeader = "INVALID_BYTES"u8.ToArray();
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(file => file.Length).Returns(invalidHeader.Length);
        fileMock.Setup(file => file.FileName).Returns("corrupt.pdf");
        fileMock.Setup(file => file.ContentType).Returns("application/pdf");
        fileMock.Setup(file => file.OpenReadStream()).Returns(() => new MemoryStream(invalidHeader));

        var command = new SaveResumeCommand(fileMock.Object, "My Resume", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error => error.ErrorMessage.Contains("File content does not match its declared type"));
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsNull_PassesValidation()
    {
        var command = new SaveResumeCommand(CreateValidPdfFileMock().Object, null, false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsWhitespace_PassesValidation()
    {
        var command = new SaveResumeCommand(CreateValidPdfFileMock().Object, "   ", false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenDisplayNameIsExactMaxLimit_PassesValidation()
    {
        var exactMaxLengthName = new string('a', Resume.MaxDisplayNameLength);
        var command = new SaveResumeCommand(CreateValidPdfFileMock().Object, exactMaxLengthName, false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenDisplayNameExceedsMaxLimit_FailsValidation()
    {
        var tooLongName = new string('a', Resume.MaxDisplayNameLength + 1);
        var command = new SaveResumeCommand(CreateValidPdfFileMock().Object, tooLongName, false);

        var validationResult = await _validator.ValidateAsync(command);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error =>
            error.PropertyName == nameof(SaveResumeCommand.DisplayName) &&
            error.ErrorMessage.Contains($"Resume name cannot exceed {Resume.MaxDisplayNameLength} characters"));
    }
}
