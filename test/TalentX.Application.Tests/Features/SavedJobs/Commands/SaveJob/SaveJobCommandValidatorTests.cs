using FluentAssertions;
using TalentX.Application.Features.SavedJobs.Commands.SaveJob;

namespace TalentX.Application.Tests.Features.SavedJobs.Commands.SaveJob;

public class SaveJobCommandValidatorTests
{
    private readonly SaveJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenJobIdIsEmpty_ShouldHaveError()
    {
        var command = new SaveJobCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "JobId");
    }

    [Fact]
    public void Validate_WhenJobIdIsValid_ShouldNotHaveError()
    {
        var command = new SaveJobCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
