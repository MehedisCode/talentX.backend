using FluentValidation;

namespace TalentX.Application.Features.Talents.Commands.RequestMediaMultipartPartUrl;

public sealed class RequestMediaMultipartPartUrlCommandValidator : AbstractValidator<RequestMediaMultipartPartUrlCommand>
{
    private const int MaxPartNumber = 10;

    public RequestMediaMultipartPartUrlCommandValidator()
    {
        RuleFor(command => command.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.");

        RuleFor(command => command.UploadId)
            .NotEmpty().WithMessage("Upload ID is required.");

        RuleFor(command => command.PartNumber)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(MaxPartNumber)
            .WithMessage($"Part number must be between 1 and {MaxPartNumber} (5 MB parts, 50 MB max video size).");
    }
}
