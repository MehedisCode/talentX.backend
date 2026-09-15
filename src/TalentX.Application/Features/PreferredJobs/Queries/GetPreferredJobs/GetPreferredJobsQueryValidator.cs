using FluentValidation;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.PreferredJobs.Queries.GetPreferredJobs;

public sealed class GetPreferredJobsQueryValidator : AbstractValidator<GetPreferredJobsQuery>
{
    public GetPreferredJobsQueryValidator()
    {
        RuleFor(x => x.Parameters).NotNull();
        When(x => x.Parameters != null, () =>
        {
            RuleFor(x => x.Parameters.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Parameters.PageSize).GreaterThan(0).LessThanOrEqualTo(PaginationRequest.MaxPageSize);
        });
    }
}
