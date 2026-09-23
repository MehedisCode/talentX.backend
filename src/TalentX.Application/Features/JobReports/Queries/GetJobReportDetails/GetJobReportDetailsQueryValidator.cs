using FluentValidation;

namespace TalentX.Application.Features.JobReports.Queries.GetJobReportDetails;

public sealed class GetJobReportDetailsQueryValidator : AbstractValidator<GetJobReportDetailsQuery>
{
    public GetJobReportDetailsQueryValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty();
    }
}
