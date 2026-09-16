using FluentValidation;

namespace TalentX.Application.Features.AdminEmployerVerification.Commands.ApproveEmployer;

public sealed class ApproveEmployerCommandValidator : AbstractValidator<ApproveEmployerCommand>
{
    public ApproveEmployerCommandValidator()
    {
        RuleFor(x => x.EmployerId)
            .NotEmpty().WithMessage("Employer ID is required.");
    }
}
