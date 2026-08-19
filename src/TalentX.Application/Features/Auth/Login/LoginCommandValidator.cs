using FluentValidation;
using TalentX.Application.Features.Auth.LogIn;

namespace TalentX.Application.Features.Auth.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .Must(email => !string.IsNullOrWhiteSpace(email))
                .WithMessage("Email is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.")
            .Must(BeValidEmail)
                .WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .Must(password => password is not null && !string.IsNullOrEmpty(password))
                .WithMessage("Password is required.")
            .MaximumLength(256).WithMessage("Password must not exceed 256 characters.");
    }

    private static bool BeValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256)
        {
            return false;
        }

        if (email.Contains(' '))
        {
            return false;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex != email.LastIndexOf('@'))
        {
            return false;
        }

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];

        if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        if (domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains(".."))
        {
            return false;
        }

        return true;
    }
}