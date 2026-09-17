using FluentValidation;
using TalentX.Application.Features.Jobs.Moderation;

namespace TalentX.Application.Features.Jobs.Queries.GetAdminJobs;

public sealed class GetAdminJobsQueryValidator : AbstractValidator<GetAdminJobsQuery>
{
    public GetAdminJobsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize must be at least 1.");

        RuleFor(query => query.SearchQuery)
            .MaximumLength(200)
            .WithMessage("Search query must be 200 characters or fewer.");

        // This is what enforces "admins only moderate published jobs": filtering in the
        // repository alone would let ?status=Draft through as a valid, empty result.
        RuleFor(query => query.Status!.Value)
            .Must(ModerationStatuses.Contains)
            .When(query => query.Status.HasValue)
            .WithMessage("Status filter must be Published, Deactivated or PendingReview.");
    }
}
