using FluentValidation;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.RejectEmployer;

public sealed class RejectEmployerCommandValidator : AbstractValidator<RejectEmployerCommand>
{
    public RejectEmployerCommandValidator()
    {
        RuleFor(x => x.EmployerId)
            .NotEmpty().WithMessage("Employer ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}
