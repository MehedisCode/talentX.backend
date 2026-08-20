using FluentValidation;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

/// <summary>
/// Validator for <see cref="GoogleSignInEmployerCommand"/>.
/// </summary>
public sealed class GoogleSignInEmployerCommandValidator : AbstractValidator<GoogleSignInEmployerCommand>
{
    public GoogleSignInEmployerCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}
