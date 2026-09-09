using FluentValidation;

namespace TalentX.Application.Features.Auth.AddPassword;

public sealed class AddPasswordCommandValidator : AbstractValidator<AddPasswordCommand>
{
    public AddPasswordCommandValidator()
    {
        RuleFor(command => command.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(256).WithMessage("New password must not exceed 256 characters.")
            .Matches(@"[A-Z]").WithMessage("New password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("New password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("New password must contain at least one digit.")
            .Matches(@"[\!\?\*\@\#\$\%\^\&\+\=]").WithMessage("New password must contain at least one special character.");

        RuleFor(command => command.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(command => command.NewPassword).WithMessage("Passwords do not match.");
    }
}
