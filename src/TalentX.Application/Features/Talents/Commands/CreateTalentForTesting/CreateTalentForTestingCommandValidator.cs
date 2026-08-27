using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.CreateTalentForTesting;

public sealed class CreateTalentForTestingCommandValidator : AbstractValidator<CreateTalentForTestingCommand>
{
    public CreateTalentForTestingCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty().WithMessage("UserId is required.")
            .MaximumLength(200).WithMessage("UserId must not exceed 200 characters.");

        RuleFor(command => command.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(120).WithMessage("Name must not exceed 120 characters.");

        RuleFor(command => command.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
    }
}
