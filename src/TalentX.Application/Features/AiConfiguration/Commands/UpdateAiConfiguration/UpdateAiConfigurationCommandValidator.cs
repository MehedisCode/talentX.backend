using FluentValidation;
using TalentX.Application.Features.AiConfiguration.Constants;

namespace TalentX.Application.Features.AiConfiguration.Commands.UpdateAiConfiguration;

public sealed class UpdateAiConfigurationCommandValidator : AbstractValidator<UpdateAiConfigurationCommand>
{
    public UpdateAiConfigurationCommandValidator()
    {
        RuleFor(command => command.ModelName)
            .NotEmpty().WithMessage("Model name is required.")
            .MaximumLength(AiConfigurationConstants.MaxModelNameLength)
            .WithMessage($"Model name cannot exceed {AiConfigurationConstants.MaxModelNameLength} characters.");
    }
}
