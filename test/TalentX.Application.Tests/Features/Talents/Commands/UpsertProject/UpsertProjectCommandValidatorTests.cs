using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.UpsertProject;
using TalentX.Application.Features.Talents.Constants;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertProject;

public class UpsertProjectCommandValidatorTests
{
    private readonly UpsertProjectCommandValidator _validator = new();

    private static UpsertProjectCommand CreateValidCommand() => new()
    {
        ProjectName = "TalentX Platform",
        Role = "Full Stack Engineer",
        ProjectType = "Professional",
        StartDate = "2023",
        EndDate = "2024",
        IsOngoing = false,
        Description = "Built scalable AI talent platform.",
        TechnologiesUsed = ["C#", ".NET", "PostgreSQL"],
        ProjectUrl = "https://talentx.ai"
    };

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var command = CreateValidCommand();
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("professional")]
    [InlineData("Personal")]
    [InlineData("ACADEMIC")]
    public void Validate_WithAllowedProjectTypeCaseInsensitive_Passes(string projectType)
    {
        var command = CreateValidCommand() with { ProjectType = projectType };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidProjectType_HasError()
    {
        var command = CreateValidCommand() with { ProjectType = "Volunteer" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.ProjectType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingProjectName_HasError(string? projectName)
    {
        var command = CreateValidCommand() with { ProjectName = projectName! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.ProjectName));
    }

    [Fact]
    public void Validate_WithExceededProjectNameLength_HasError()
    {
        var command = CreateValidCommand() with { ProjectName = new string('A', ProjectConstants.MaxProjectNameLength + 1) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.ProjectName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingRole_HasError(string? role)
    {
        var command = CreateValidCommand() with { Role = role! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.Role));
    }

    [Fact]
    public void Validate_WithExceededRoleLength_HasError()
    {
        var command = CreateValidCommand() with { Role = new string('A', ProjectConstants.MaxRoleLength + 1) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.Role));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingStartDate_HasError(string? startDate)
    {
        var command = CreateValidCommand() with { StartDate = startDate! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.StartDate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingDescription_HasError(string? description)
    {
        var command = CreateValidCommand() with { Description = description! };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.Description));
    }

    [Fact]
    public void Validate_WithExceededDescriptionLength_HasError()
    {
        var command = CreateValidCommand() with { Description = new string('A', ProjectConstants.MaxDescriptionLength + 1) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.Description));
    }

    [Fact]
    public void Validate_WithTooManyTechnologies_HasError()
    {
        var technologies = Enumerable.Range(1, ProjectConstants.MaxTechnologiesCount + 1).Select(i => $"Tech{i}").ToList();
        var command = CreateValidCommand() with { TechnologiesUsed = technologies };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.TechnologiesUsed));
    }

    [Fact]
    public void Validate_WithExceededTechnologyTagLength_HasError()
    {
        var command = CreateValidCommand() with { TechnologiesUsed = [new string('X', ProjectConstants.MaxTechnologyTagLength + 1)] };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.StartsWith(nameof(UpsertProjectCommand.TechnologiesUsed)));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://files.example.com")]
    [InlineData("file:///local/path")]
    public void Validate_WithInvalidProjectUrl_HasError(string url)
    {
        var command = CreateValidCommand() with { ProjectUrl = url };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertProjectCommand.ProjectUrl));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://github.com/my-project")]
    [InlineData("http://demo.example.com/app?test=1")]
    public void Validate_WithValidOrEmptyProjectUrl_Passes(string? url)
    {
        var command = CreateValidCommand() with { ProjectUrl = url };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
