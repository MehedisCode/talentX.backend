using FluentAssertions;
using TalentX.Application.Features.TalentMatching.Commands.RunJobTalentMatching;
using Xunit;

namespace TalentX.Application.Tests.Features.TalentMatching;

public class RunJobTalentMatchingCommandValidatorTests
{
    private readonly RunJobTalentMatchingCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenJobIdIsEmpty_HasValidationError()
    {
        var command = new RunJobTalentMatchingCommand(Guid.Empty);
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "JobId");
    }

    [Fact]
    public void Validate_WhenJobIdIsValid_PassesValidation()
    {
        var command = new RunJobTalentMatchingCommand(Guid.NewGuid());
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
