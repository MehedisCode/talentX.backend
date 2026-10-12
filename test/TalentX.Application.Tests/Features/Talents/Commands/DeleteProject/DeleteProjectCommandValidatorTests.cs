using FluentAssertions;
using TalentX.Application.Features.Talents.Commands.DeleteProject;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.DeleteProject;

public class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyId_HasError()
    {
        var result = _validator.Validate(new DeleteProjectCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidId_Passes()
    {
        var result = _validator.Validate(new DeleteProjectCommand(Guid.NewGuid()));

        result.IsValid.Should().BeTrue();
    }
}
