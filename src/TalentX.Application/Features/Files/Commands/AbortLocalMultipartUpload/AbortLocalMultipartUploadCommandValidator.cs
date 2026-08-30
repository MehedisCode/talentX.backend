using FluentValidation;

namespace TalentX.Application.Features.Files.Commands.AbortLocalMultipartUpload;

public sealed class AbortLocalMultipartUploadCommandValidator : AbstractValidator<AbortLocalMultipartUploadCommand>
{
    public AbortLocalMultipartUploadCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty()
            .WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty()
            .WithMessage("Upload ID is required.");
    }
}
