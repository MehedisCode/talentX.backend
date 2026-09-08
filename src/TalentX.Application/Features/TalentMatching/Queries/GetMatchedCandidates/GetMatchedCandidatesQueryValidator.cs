using FluentValidation;

namespace TalentX.Application.Features.TalentMatching.Queries.GetMatchedCandidates;

public sealed class GetMatchedCandidatesQueryValidator : AbstractValidator<GetMatchedCandidatesQuery>
{
    public GetMatchedCandidatesQueryValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty()
            .WithMessage("Job ID is required.");
    }
}
