using FluentValidation.TestHelper;
using TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;
using Xunit;

namespace TalentX.Application.Tests.Features.AiConfiguration;

public sealed class UpdateAiConfigurationCommandValidatorTests
{
    private readonly UpdateAiConfigurationCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ModelName_WhenEmptyOrNull_HasValidationError(string? invalidModelName)
    {
        var command = new UpdateAiConfigurationCommand(invalidModelName!, "api-key");

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.ModelName)
            .WithErrorMessage("Model name is required.");
    }

    [Fact]
    public void ModelName_WhenExceedsMaxLength_HasValidationError()
    {
        var longModelName = new string('a', 101);
        var command = new UpdateAiConfigurationCommand(longModelName, "api-key");

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldHaveValidationErrorFor(c => c.ModelName)
            .WithErrorMessage("Model name cannot exceed 100 characters.");
    }

    [Fact]
    public void ModelName_WhenValid_PassesValidation()
    {
        var command = new UpdateAiConfigurationCommand("gemini-2.0-flash", "api-key");

        var validationResult = _validator.TestValidate(command);

        validationResult.ShouldNotHaveValidationErrorFor(c => c.ModelName);
    }
}
