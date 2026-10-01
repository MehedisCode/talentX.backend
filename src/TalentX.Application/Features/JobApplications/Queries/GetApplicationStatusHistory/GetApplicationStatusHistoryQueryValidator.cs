using FluentValidation;

namespace TalentX.Application.Features.JobApplications.Queries.GetApplicationStatusHistory;

public sealed class GetApplicationStatusHistoryQueryValidator : AbstractValidator<GetApplicationStatusHistoryQuery>
{
    public GetApplicationStatusHistoryQueryValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("ApplicationId is required.");
    }
}
