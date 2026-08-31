using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.UpsertWorkExperience;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertWorkExperience;

public class UpsertWorkExperienceCommandValidatorTests
{
    private readonly UpsertWorkExperienceCommandValidator _validator = new();

    [Fact]
    public void Validate_WithMissingCompanyName_HasError()
    {
        var result = _validator.Validate(new UpsertWorkExperienceCommand(null, string.Empty, "Engineer", null, null, false));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new UpsertWorkExperienceCommand(null, "Acme", "Engineer", "2020", "Present", true));

        result.IsValid.Should().BeTrue();
    }
}
