using FluentValidation;

namespace TalentX.Application.Features.Resumes.Queries.GetResumeUrl;

public sealed class GetResumeUrlQueryValidator : AbstractValidator<GetResumeUrlQuery>
{
    public GetResumeUrlQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
