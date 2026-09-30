using FluentValidation;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;

public sealed class GetEmployerCompanyReviewsQueryValidator : AbstractValidator<GetEmployerCompanyReviewsQuery>
{
    public GetEmployerCompanyReviewsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize must be at least 1.");

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
