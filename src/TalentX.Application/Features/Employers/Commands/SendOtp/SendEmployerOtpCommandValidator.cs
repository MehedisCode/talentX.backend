using FluentValidation;

namespace TalentX.Application.Features.Employers.Commands.SendOtp;

public sealed class SendEmployerOtpCommandValidator : AbstractValidator<SendEmployerOtpCommand>
{
    public SendEmployerOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
    }
}
