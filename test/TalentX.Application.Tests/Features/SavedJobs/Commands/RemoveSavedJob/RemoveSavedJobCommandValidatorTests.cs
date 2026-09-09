using FluentAssertions;
using TalentX.Application.Features.SavedJobs.Commands.RemoveSavedJob;

namespace TalentX.Application.Tests.Features.SavedJobs.Commands.RemoveSavedJob;

public class RemoveSavedJobCommandValidatorTests
{
    private readonly RemoveSavedJobCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenJobIdIsEmpty_ShouldHaveError()
    {
        var command = new RemoveSavedJobCommand(Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "JobId");
    }

    [Fact]
    public void Validate_WhenJobIdIsValid_ShouldNotHaveError()
    {
        var command = new RemoveSavedJobCommand(Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
