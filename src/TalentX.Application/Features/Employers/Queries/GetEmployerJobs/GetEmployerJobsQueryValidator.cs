using FluentValidation;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;

namespace TalentX.Application.Features.Employers.Queries.GetEmployerJobs;

public sealed class GetEmployerJobsQueryValidator : AbstractValidator<GetEmployerJobsQuery>
{
    public GetEmployerJobsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRequest.MaxPageSize}.");

        RuleFor(query => query.SearchTerm)
            .MaximumLength(EmployerDashboardConstants.MaxSearchTermLength)
            .WithMessage($"SearchTerm must not exceed {EmployerDashboardConstants.MaxSearchTermLength} characters.")
            .When(query => query.SearchTerm is not null);
    }
}
