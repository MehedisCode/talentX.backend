using FluentValidation;
using TalentX.Application.Common.Models;

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
    }
}
