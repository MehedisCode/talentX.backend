using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.UpsertEducation;

public sealed class UpsertEducationCommandValidator : AbstractValidator<UpsertEducationCommand>
{
    public UpsertEducationCommandValidator()
    {
        RuleFor(command => command.Institution)
            .NotEmpty().WithMessage("Institution is required.")
            .MaximumLength(200).WithMessage("Institution must not exceed 200 characters.");

        RuleFor(command => command.Degree)
            .NotEmpty().WithMessage("Degree is required.")
            .MaximumLength(200).WithMessage("Degree must not exceed 200 characters.");

        RuleFor(command => command.StartDate)
            .MaximumLength(50).WithMessage("Start date must not exceed 50 characters.");

        RuleFor(command => command.EndDate)
            .MaximumLength(50).WithMessage("End date must not exceed 50 characters.");
    }
}
