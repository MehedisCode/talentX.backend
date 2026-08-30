using FluentValidation;

namespace TalentX.Application.Features.Files.Commands.SaveLocalMultipartPart;

public sealed class SaveLocalMultipartPartCommandValidator : AbstractValidator<SaveLocalMultipartPartCommand>
{
    public SaveLocalMultipartPartCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty()
            .WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty()
            .WithMessage("Upload ID is required.");

        RuleFor(command => command.PartNumber)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(10000)
            .WithMessage("Part number must be between 1 and 10000.");

        RuleFor(command => command.ContentStream)
            .NotNull()
            .WithMessage("Content stream is required.");
    }
}
