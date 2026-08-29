using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.ExtractResume;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ExtractResume;

public class ExtractResumeCommandValidatorTests
{
    private readonly ExtractResumeCommandValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFileIsNull()
    {
        var command = new ExtractResumeCommand(null!);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenFileSizeExceedsLimit()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var command = new ExtractResumeCommand(fileMock.Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("exceed 5 MB"));
    }

    [Fact]
    public async Task Validate_ShouldHaveError_WhenExtensionIsNotSupported()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.FileName).Returns("resume.txt");
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        var command = new ExtractResumeCommand(fileMock.Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PDF and DOCX"));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenMagicBytesDoNotMatchExtension()
    {
        var fakeHeader = "INVALID_HEADER_BYTES"u8.ToArray();
        var stream = new MemoryStream(fakeHeader);

        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(fakeHeader.Length);
        fileMock.Setup(f => f.FileName).Returns("fake.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var command = new ExtractResumeCommand(fileMock.Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not match its declared type"));
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenValidPdfWithMagicBytes()
    {
        var pdfHeader = "%PDF-1.4 header bytes"u8.ToArray();
        var stream = new MemoryStream(pdfHeader);

        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.Length).Returns(pdfHeader.Length);
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var command = new ExtractResumeCommand(fileMock.Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
