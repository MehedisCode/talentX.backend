using FluentValidation;

namespace TalentX.Application.Features.Auth.Commands.VerifyGuestEmail;

public sealed class VerifyGuestEmailCommandValidator : AbstractValidator<VerifyGuestEmailCommand>
{
    public VerifyGuestEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}
