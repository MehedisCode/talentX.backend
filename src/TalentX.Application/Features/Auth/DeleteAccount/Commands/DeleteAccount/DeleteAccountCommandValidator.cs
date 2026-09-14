using FluentValidation;

namespace TalentX.Application.Features.Auth.DeleteAccount.Commands.DeleteAccount;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("Verification code is required.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must be 6 digits.");
    }
}
