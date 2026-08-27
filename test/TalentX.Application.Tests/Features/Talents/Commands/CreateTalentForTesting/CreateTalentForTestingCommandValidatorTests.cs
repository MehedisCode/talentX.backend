using FluentValidation.TestHelper;
using TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;
using Xunit;

namespace TalentX.Application.Tests.Features.Talents.Commands.CreateTalentForTesting;

public class CreateTalentForTestingCommandValidatorTests
{
    private readonly CreateTalentForTestingCommandValidator _validator = new();

    private static CreateTalentForTestingCommand ValidCommand() => new(
        "user-1",
        "Jane Doe",
        "jane@example.com"
    );

    [Fact]
    public void Validate_WithAValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingUserId_HasError(string? userId)
    {
        var command = ValidCommand() with { UserId = userId! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithMissingName_HasError(string? name)
    {
        var command = ValidCommand() with { Name = name! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithAnInvalidEmail_HasError(string? email)
    {
        var command = ValidCommand() with { Email = email! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }
}
