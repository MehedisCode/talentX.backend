using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviews;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetAdminCompanyReviews;

public sealed class GetAdminCompanyReviewsQueryValidatorTests
{
    private readonly GetAdminCompanyReviewsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(CompanyReviewStatus.Pending)]
    [InlineData(CompanyReviewStatus.Published)]
    [InlineData(CompanyReviewStatus.Rejected)]
    [InlineData(CompanyReviewStatus.Hidden)]
    [InlineData(CompanyReviewStatus.Withdrawn)]
    public void Validate_WithAnyStatus_HasNoValidationErrors(CompanyReviewStatus status)
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { Status = status });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidStatus_HasValidationError()
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { Status = (CompanyReviewStatus)999 });

        result.ShouldHaveValidationErrorFor(query => query.Status!.Value)
            .WithErrorMessage("Invalid Status.");
    }

    [Fact]
    public void Validate_WithInvalidSentiment_HasValidationError()
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { Sentiment = (ReviewSentiment)999 });

        result.ShouldHaveValidationErrorFor(query => query.Sentiment!.Value)
            .WithErrorMessage("Invalid Sentiment.");
    }

    [Fact]
    public void Validate_WhenSearchQueryExceeds200Characters_HasValidationError()
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { SearchQuery = new string('a', 201) });

        result.ShouldHaveValidationErrorFor(query => query.SearchQuery)
            .WithErrorMessage("Search query must be 200 characters or fewer.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsLessThanOne_HasValidationError(int invalidPage)
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { Page = invalidPage });

        result.ShouldHaveValidationErrorFor(query => query.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenPageSizeIsLessThanOne_HasValidationError(int invalidPageSize)
    {
        var result = _validator.TestValidate(new GetAdminCompanyReviewsQuery { PageSize = invalidPageSize });

        result.ShouldHaveValidationErrorFor(query => query.PageSize)
            .WithErrorMessage("PageSize must be at least 1.");
    }
}
