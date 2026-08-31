using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.DeleteEducation;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteEducation;

public class DeleteEducationCommandValidatorTests
{
    private readonly DeleteEducationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyId_HasError()
    {
        var result = _validator.Validate(new DeleteEducationCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        var result = _validator.Validate(new DeleteEducationCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
