using FluentValidation;

namespace TalentX.Application.Features.Employers.Commands.ResendOtp;

/// <summary>
/// Validator for <see cref="ResendEmployerOtpCommand"/>.
/// </summary>
public sealed class ResendEmployerOtpCommandValidator : AbstractValidator<ResendEmployerOtpCommand>
{
    public ResendEmployerOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
    }
}
