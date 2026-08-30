using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.CompleteMediaMultipartUpload;

public sealed class CompleteMediaMultipartUploadCommandValidator : AbstractValidator<CompleteMediaMultipartUploadCommand>
{
    public CompleteMediaMultipartUploadCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty().WithMessage("Upload ID is required.");

        RuleFor(command => command.Parts)
            .NotEmpty().WithMessage("At least one uploaded part is required to complete the upload.");

        RuleForEach(command => command.Parts).ChildRules(part =>
        {
            part.RuleFor(item => item.PartNumber).GreaterThanOrEqualTo(1);
            part.RuleFor(item => item.ETag).NotEmpty().WithMessage("Part ETag is required.");
        });
    }
}
