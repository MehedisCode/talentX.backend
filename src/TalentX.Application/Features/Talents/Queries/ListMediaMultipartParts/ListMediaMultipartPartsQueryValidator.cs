using FluentValidation;

namespace TalentX.Application.Features.Talents.Queries.ListMediaMultipartParts;

public sealed class ListMediaMultipartPartsQueryValidator : AbstractValidator<ListMediaMultipartPartsQuery>
{
    public ListMediaMultipartPartsQueryValidator()
    {
        RuleFor(query => query.StorageKey)
            .NotEmpty().WithMessage("Storage key is required.");

        RuleFor(query => query.UploadId)
            .NotEmpty().WithMessage("Upload ID is required.");
    }
}
