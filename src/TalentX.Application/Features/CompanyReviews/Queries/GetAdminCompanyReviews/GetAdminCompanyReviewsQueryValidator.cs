using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;

public sealed class GetAdminCompanyReviewsQueryValidator : AbstractValidator<GetAdminCompanyReviewsQuery>
{
    public GetAdminCompanyReviewsQueryValidator()
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

        RuleFor(query => query.Status!.Value)
            .IsInEnum()
            .When(query => query.Status.HasValue)
            .WithMessage("Invalid Status.");

        RuleFor(query => query.Sentiment!.Value)
            .IsInEnum()
            .When(query => query.Sentiment.HasValue)
            .WithMessage("Invalid Sentiment.");
    }
}
