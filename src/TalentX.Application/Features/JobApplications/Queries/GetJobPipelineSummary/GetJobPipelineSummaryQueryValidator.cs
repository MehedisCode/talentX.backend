namespace TalentX.Application.Features.JobApplications.Queries.GetJobPipelineSummary;

using FluentValidation;

public sealed class GetJobPipelineSummaryQueryValidator : AbstractValidator<GetJobPipelineSummaryQuery>
{
    public GetJobPipelineSummaryQueryValidator()
    {
        RuleFor(query => query.JobId)
            .NotEmpty()
            .WithMessage("JobId is required.");
    }
}
