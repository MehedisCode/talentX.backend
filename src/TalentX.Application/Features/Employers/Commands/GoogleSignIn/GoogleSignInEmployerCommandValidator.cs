using FluentValidation;

namespace TalentX.Application.Features.Employers.Commands.GoogleSignIn;

public sealed class GoogleSignInEmployerCommandValidator : AbstractValidator<GoogleSignInEmployerCommand>
{
    public GoogleSignInEmployerCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID token is required.");
    }
}
