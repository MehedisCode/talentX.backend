using FluentValidation;

namespace TalentX.Application.Features.Auth.ValidateResetToken;

public sealed class ValidateResetTokenQueryValidator : AbstractValidator<ValidateResetTokenQuery>
{
    public ValidateResetTokenQueryValidator()
    {
        RuleFor(query => query.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(query => query.Token)
            .NotEmpty().WithMessage("Reset token is required.");
    }
}
