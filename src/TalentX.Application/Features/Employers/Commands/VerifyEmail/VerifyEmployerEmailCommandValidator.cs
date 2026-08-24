using FluentValidation;

namespace TalentX.Application.Features.Employers.Commands.VerifyEmail;

public sealed class VerifyEmployerEmailCommandValidator : AbstractValidator<VerifyEmployerEmailCommand>
{
    public VerifyEmployerEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP code is required.")
            .Length(6).WithMessage("OTP code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("OTP code must contain numeric digits only.");
    }
}
