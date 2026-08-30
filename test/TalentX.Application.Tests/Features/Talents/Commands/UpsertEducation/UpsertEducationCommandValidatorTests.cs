using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.UpsertEducation;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.UpsertEducation;

public class UpsertEducationCommandValidatorTests
{
    private readonly UpsertEducationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithMissingInstitution_HasError()
    {
        var result = _validator.Validate(new UpsertEducationCommand(null, string.Empty, "BSc", null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var result = _validator.Validate(new UpsertEducationCommand(null, "State University", "BSc", "2016", "2020"));

        result.IsValid.Should().BeTrue();
    }
}
