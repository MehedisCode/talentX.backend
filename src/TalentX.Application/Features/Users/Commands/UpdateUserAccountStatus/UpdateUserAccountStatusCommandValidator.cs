using FluentValidation;

namespace TalentX.Application.Features.Users.Commands.UpdateUserAccountStatus;

public sealed class UpdateUserAccountStatusCommandValidator : AbstractValidator<UpdateUserAccountStatusCommand>
{
    public UpdateUserAccountStatusCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(command => command.NewStatus)
            .IsInEnum()
            .WithMessage("Invalid account status.");
    }
}
