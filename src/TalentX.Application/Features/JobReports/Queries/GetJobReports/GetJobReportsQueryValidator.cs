using FluentValidation;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReports;

public sealed class GetJobReportsQueryValidator : AbstractValidator<GetJobReportsQuery>
{
    public GetJobReportsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(PaginationRequest.MaxPageSize);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}
