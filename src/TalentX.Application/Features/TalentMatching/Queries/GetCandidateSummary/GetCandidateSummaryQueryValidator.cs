using FluentValidation;

namespace TalentX.Application.Features.TalentMatching.Queries.GetCandidateSummary;

public sealed class GetCandidateSummaryQueryValidator : AbstractValidator<GetCandidateSummaryQuery>
{
    public GetCandidateSummaryQueryValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty()
            .WithMessage("Candidate identifier is required.");
    }
}
