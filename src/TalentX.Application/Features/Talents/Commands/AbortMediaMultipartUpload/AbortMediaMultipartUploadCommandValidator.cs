using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.AbortMediaMultipartUpload;

public sealed class AbortMediaMultipartUploadCommandValidator : AbstractValidator<AbortMediaMultipartUploadCommand>
{
    public AbortMediaMultipartUploadCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty().WithMessage("Upload ID is required.");
    }
}
