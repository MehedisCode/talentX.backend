using FluentValidation;

namespace TalentX.Application.Features.Files.Queries.GetLocalFileStream;

public sealed class GetLocalFileStreamQueryValidator : AbstractValidator<GetLocalFileStreamQuery>
{
    public GetLocalFileStreamQueryValidator()
    {
        RuleFor(query => query.StorageKey)
            .NotEmpty()
            .WithMessage("Storage key is required.");
    }
}
