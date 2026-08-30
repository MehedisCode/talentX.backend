using FluentValidation;

namespace TalentX.Application.Features.Files.Commands.CompleteLocalMultipartUpload;

public sealed class CompleteLocalMultipartUploadCommandValidator : AbstractValidator<CompleteLocalMultipartUploadCommand>
{
    public CompleteLocalMultipartUploadCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty()
            .WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty()
            .WithMessage("Upload ID is required.");
    }
}
