using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.ConfirmProfileMediaUpload;

public sealed class ConfirmProfileMediaUploadCommandValidator : AbstractValidator<ConfirmProfileMediaUploadCommand>
{
    public ConfirmProfileMediaUploadCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.")
            .MaximumLength(500).WithMessage("Storage key must not exceed 500 characters.");
    }
}
