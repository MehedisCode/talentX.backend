using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.Commands.Apply;
using Xunit;

namespace TalentX.Application.Tests.Features.JobApplications.Commands.Apply;

public class ApplyToJobCommandValidatorTests
{
    private readonly ApplyToJobCommandValidator _validator = new();

    private static Mock<IUploadedFile> MockResumeFile(string fileName = "resume.pdf")
    {
        var fileMock = new Mock<IUploadedFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1024);
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream([0x25, 0x50, 0x44, 0x46]));
        return fileMock;
    }

    private static ApplyToJobCommand ValidCommand(IUploadedFile? file = null) => new(
        Guid.NewGuid(),
        "Jane Doe",
        "jane@example.com",
        file ?? MockResumeFile().Object,
        "I am excited to apply for this role.",
        85000,
        6,
        "Dhaka"
    );

    [Fact]
    public async Task Validate_WithAValidCommand_HasNoErrors()
    {
        var result = await _validator.TestValidateAsync(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithEmptyJobId_HasError()
    {
        var command = ValidCommand() with { JobId = Guid.Empty };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.JobId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("J")]
    public async Task Validate_WithInvalidName_HasError(string? name)
    {
        var command = ValidCommand() with { ApplicantName = name! };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Fact]
    public async Task Validate_WithNameOver120Characters_HasError()
    {
        var command = ValidCommand() with { ApplicantName = new string('a', 121) };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName);
    }

    [Fact]
    public async Task Validate_WithNameContainingNoLetters_HasError()
    {
        var command = ValidCommand() with { ApplicantName = "12345678" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantName)
            .WithErrorMessage("Name must contain at least one letter.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_WithMissingEmail_HasError(string? email)
    {
        var command = ValidCommand() with { ApplicantEmail = email! };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("jane@")]
    [InlineData("@example.com")]
    public async Task Validate_WithInvalidEmail_HasError(string email)
    {
        var command = ValidCommand() with { ApplicantEmail = email };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public async Task Validate_WithSuccessfulEmail_HasNoError()
    {
        var command = ValidCommand() with { ApplicantEmail = "jane.doe+work@example.co.uk" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Fact]
    public async Task Validate_WithEmailOver255Characters_HasError()
    {
        var command = ValidCommand() with { ApplicantEmail = $"jane@{new string('a', 250)}.com" };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicantEmail);
    }

    [Fact]
    public async Task Validate_WithMissingResumeFile_HasNoError()
    {
        var command = ValidCommand() with { ResumeFile = null };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ResumeFile);
    }

    [Fact]
    public async Task Validate_WithPresentResumeFileOfUnsupportedType_HasError()
    {
        var command = ValidCommand(file: MockResumeFile("resume.txt").Object);

        var result = await _validator.TestValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Only PDF and DOCX files are accepted.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("T")]
    public async Task Validate_WithInvalidCoverLetter_HasError(string? coverLetter)
    {
        var command = ValidCommand() with { CoverLetter = coverLetter! };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter);
    }

    [Fact]
    public async Task Validate_WithCoverLetterOver4000Characters_HasError()
    {
        var command = ValidCommand() with { CoverLetter = new string('a', 4001) };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter);
    }

    [Fact]
    public async Task Validate_WithCoverLetterThatIsAllWhitespace_HasError()
    {
        var command = ValidCommand() with { CoverLetter = new string(' ', 25) };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CoverLetter)
            .WithErrorMessage("Cover letter must be at least 2 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task Validate_WithNonPositiveExpectedSalary_HasError(decimal salary)
    {
        var command = ValidCommand() with { ExpectedSalary = salary };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Fact]
    public async Task Validate_WithPositiveExpectedSalary_HasNoError()
    {
        var command = ValidCommand() with { ExpectedSalary = 50000 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ExpectedSalary);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(81)]
    public async Task Validate_WithOutOfRangeExperience_HasError(decimal years)
    {
        var command = ValidCommand() with { TotalYearsExperience = years };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public async Task Validate_WithZeroExperience_HasNoError()
    {
        var command = ValidCommand() with { TotalYearsExperience = 0 };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.TotalYearsExperience);
    }

    [Fact]
    public async Task Validate_WithLocationOver200Characters_HasError()
    {
        var command = ValidCommand() with { Location = new string('a', 201) };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.Location);
    }

    [Fact]
    public async Task Validate_WithNullLocation_HasNoError()
    {
        var command = ValidCommand() with { Location = null };

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Location);
    }
}