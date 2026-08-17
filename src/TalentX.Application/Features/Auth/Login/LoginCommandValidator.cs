using FluentValidation;
using TalentX.Application.Features.Auth.LogIn;

namespace TalentX.Application.Features.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        // As we are using 'Identity'
        // Password should have atleast 8 characters,
        // must contain at least 1 uppercase letter [A-Z],
        // 1 lowercase letter [a-z] and 1 digit [0-9]
        // 1 special character
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}