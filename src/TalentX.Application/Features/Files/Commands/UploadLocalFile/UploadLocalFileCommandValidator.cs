using FluentValidation;

namespace TalentX.Application.Features.Files.Commands.UploadLocalFile;

public sealed class UploadLocalFileCommandValidator : AbstractValidator<UploadLocalFileCommand>
{
    public UploadLocalFileCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty()
            .WithMessage("Storage key is required.");

        RuleFor(command => command.ContentStream)
            .NotNull()
            .WithMessage("Content stream is required.");
    }
}
