using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.DeleteWorkExperience;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteWorkExperience;

public class DeleteWorkExperienceCommandValidatorTests
{
    private readonly DeleteWorkExperienceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyId_HasError()
    {
        var result = _validator.Validate(new DeleteWorkExperienceCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        var result = _validator.Validate(new DeleteWorkExperienceCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
