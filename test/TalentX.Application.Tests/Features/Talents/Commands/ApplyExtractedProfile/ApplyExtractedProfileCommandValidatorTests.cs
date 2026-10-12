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

    private static ApplyExtractedProfileData ValidProfileData() => new(
        "Jane Smith",
        "Senior Cloud Architect",
        "Experienced architect with expertise in cloud systems.",
        "San Francisco, CA",
        "+14155552671",
        "https://www.linkedin.com/in/janesmith",
        ["https://github.com/janesmith"],
        8.5m,
        140000m,
        ["High velocity delivery"],
        ["C#"],
        ["Teamwork"],
        ["Docker"],
        [new ApplyExtractedWorkExperienceItem("Acme", "Engineer", "2020", "Present", true)],
        [new ApplyExtractedEducationItem("State University", "BSc", "2016", "2020")]
    );

    private static ApplyExtractedProfileCommand ValidCommand() => new(
        ValidProfileData(),
        ValidFileMock().Object
    );

    [Fact]
    public async Task Validate_WithValidCommand_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenProfileDataIsNull_HasError()
    {
        var command = new ApplyExtractedProfileCommand(null!, ValidFileMock().Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProfileData");
    }

    [Fact]
    public async Task Validate_WhenProfileFieldsAreNull_Passes()
    {
        var sparseProfileData = new ApplyExtractedProfileData(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var command = new ApplyExtractedProfileCommand(sparseProfileData, ValidFileMock().Object);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenFullNameExceeds120Chars_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { FullName = new string('a', 121) }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Full name must not exceed 120 characters"));
    }

    [Fact]
    public async Task Validate_WhenTaglineExceeds160Chars_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { Tagline = new string('a', 161) }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Tagline must not exceed 160 characters"));
    }

    [Fact]
    public async Task Validate_WhenShortBioExceeds1000Chars_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { ShortBio = new string('a', 1001) }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Short bio must not exceed 1000 characters"));
    }

    [Fact]
    public async Task Validate_WhenLocationExceeds200Chars_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { Location = new string('a', 201) }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Location must not exceed 200 characters"));
    }

    [Fact]
    public async Task Validate_WhenPhoneExceeds40Chars_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { Phone = new string('1', 41) }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Phone must not exceed 40 characters"));
    }

    [Fact]
    public async Task Validate_WhenLinkedInUrlExceeds500Chars_HasError()
    {
        var longUrl = $"https://linkedin.com/in/{new string('a', 500)}";
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { LinkedInUrl = longUrl }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("LinkedIn URL must not exceed 500 characters"));
    }

    [Fact]
    public async Task Validate_WhenLinkedInUrlIsInvalidUrl_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { LinkedInUrl = "not-a-valid-url" }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("LinkedIn URL must be a valid URL"));
    }

    [Fact]
    public async Task Validate_WhenExpectedSalaryIsNegative_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { ExpectedSalary = -1m }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Expected salary cannot be negative"));
    }

    [Fact]
    public async Task Validate_WhenTotalYearsExperienceIsNegative_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { TotalYearsExperience = -0.5m }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Total years of experience cannot be negative"));
    }

    [Fact]
    public async Task Validate_WhenAdditionalUrlsContainInvalidUrl_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with { AdditionalUrls = ["not-a-url"] }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Additional URLs must be valid URLs"));
    }

    [Fact]
    public async Task Validate_WhenResumeFileIsNull_Passes()
    {
        var command = ValidCommand() with { ResumeFile = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
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
            ProfileData = ValidProfileData() with
            {
                WorkExperiences = [new ApplyExtractedWorkExperienceItem(string.Empty, "Engineer", "2020", "Present", true)]
            }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WhenEducationDegreeIsMissing_HasError()
    {
        var command = ValidCommand() with
        {
            ProfileData = ValidProfileData() with
            {
                Educations = [new ApplyExtractedEducationItem("State University", string.Empty, "2016", "2020")]
            }
        };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }
}
