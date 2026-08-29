using FluentAssertions;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ApplyExtractedProfile;

public class ApplyExtractedProfileCommandValidatorTests
{
    private readonly ApplyExtractedProfileCommandValidator _validator = new();

    private static Mock<IUploadedFile> ValidFileMock()
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns("resume.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream("%PDF-1.4 header bytes"u8.ToArray()));
        return fileMock;
    }

    private static ApplyExtractedProfileCommand ValidCommand() => new(
        ["C#"],
        ["Teamwork"],
        ["Docker"],
        [new ApplyExtractedWorkExperienceItem("Acme", "Engineer", "2020", "Present", true)],
        [new ApplyExtractedEducationItem("State University", "BSc", "2016", "2020")],
        ValidFileMock().Object
    );

    [Fact]
    public async Task Validate_WithValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenResumeFileIsNull_HasError()
    {
        var command = ValidCommand() with { ResumeFile = null! };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenResumeFileExceedsSizeLimit_HasError()
    {
        var fileMock = ValidFileMock();
        fileMock.Setup(f => f.Length).Returns(6 * 1024 * 1024);
        var command = ValidCommand() with { ResumeFile = fileMock.Object };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("exceed 5 MB"));
    }

    [Fact]
    public async Task Validate_WhenResumeFileExtensionIsNotSupported_HasError()
    {
        var fileMock = ValidFileMock();
        fileMock.Setup(f => f.FileName).Returns("resume.txt");
        fileMock.Setup(f => f.ContentType).Returns("text/plain");
        var command = ValidCommand() with { ResumeFile = fileMock.Object };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("PDF and DOCX"));
    }

    [Fact]
    public async Task Validate_WhenResumeFileMagicBytesDoNotMatchExtension_HasError()
    {
        var fileMock = ValidFileMock();
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream("INVALID_HEADER_BYTES"u8.ToArray()));
        var command = ValidCommand() with { ResumeFile = fileMock.Object };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("does not match its declared type"));
    }

    [Fact]
    public async Task Validate_WhenWorkExperienceCompanyNameIsMissing_HasError()
    {
        var command = ValidCommand() with
        {
            WorkExperiences = [new ApplyExtractedWorkExperienceItem(string.Empty, "Engineer", "2020", "Present", true)]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenEducationDegreeIsMissing_HasError()
    {
        var command = ValidCommand() with
        {
            Educations = [new ApplyExtractedEducationItem("State University", string.Empty, "2016", "2020")]
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
