using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.ApplyExtractedProfile;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.ApplyExtractedProfile;

public class ApplyExtractedProfileCommandValidatorTests
{
    private readonly ApplyExtractedProfileCommandValidator _validator = new();

    private static ApplyExtractedProfileCommand ValidCommand() => new(
        ["C#"],
        ["Teamwork"],
        ["Docker"],
        [new ApplyExtractedWorkExperienceItem("Acme", "Engineer", "2020", "Present", true)],
        [new ApplyExtractedEducationItem("State University", "BSc", "2016", "2020")],
        "resumes/key.pdf",
        "resume.pdf",
        "application/pdf",
        1024
    );

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenResumeStorageKeyIsEmpty_HasError()
    {
        var command = ValidCommand() with { ResumeStorageKey = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResumeStorageKey");
    }

    [Fact]
    public void Validate_WhenResumeSizeBytesIsZero_HasError()
    {
        var command = ValidCommand() with { ResumeSizeBytes = 0 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResumeSizeBytes");
    }

    [Fact]
    public void Validate_WhenWorkExperienceCompanyNameIsMissing_HasError()
    {
        var command = ValidCommand() with
        {
            WorkExperiences = [new ApplyExtractedWorkExperienceItem(string.Empty, "Engineer", "2020", "Present", true)]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenEducationDegreeIsMissing_HasError()
    {
        var command = ValidCommand() with
        {
            Educations = [new ApplyExtractedEducationItem("State University", string.Empty, "2016", "2020")]
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
