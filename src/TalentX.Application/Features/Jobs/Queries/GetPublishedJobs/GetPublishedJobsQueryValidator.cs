using FluentValidation;
using TalentX.Application.Common.Models;

namespace TalentX.Application.Features.Jobs.Queries.GetPublishedJobs;

public class GetPublishedJobsQueryValidator : AbstractValidator<GetPublishedJobsQuery>
{
    public GetPublishedJobsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PaginationRequest.MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {PaginationRequest.MaxPageSize}.");

        RuleFor(x => x.Keyword)
            .MaximumLength(100).WithMessage("Keyword must not exceed 100 characters.")
            .When(x => x.Keyword is not null);

        RuleFor(x => x.Location)
            .MaximumLength(100).WithMessage("Location must not exceed 100 characters.")
            .When(x => x.Location is not null);
    }
}
