using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Queries.GetEmployerCompanyReviews;

public sealed class GetEmployerCompanyReviewsQueryValidatorTests
{
    private readonly GetEmployerCompanyReviewsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultQuery_HasNoValidationErrors()
    {
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery());

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
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery { Status = status });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithInvalidStatus_HasValidationError()
    {
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery { Status = (CompanyReviewStatus)999 });

        result.ShouldHaveValidationErrorFor(query => query.Status!.Value)
            .WithErrorMessage("Invalid Status.");
    }

    [Fact]
    public void Validate_WithInvalidSentiment_HasValidationError()
    {
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery { Sentiment = (ReviewSentiment)999 });

        result.ShouldHaveValidationErrorFor(query => query.Sentiment!.Value)
            .WithErrorMessage("Invalid Sentiment.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPageIsLessThanOne_HasValidationError(int invalidPage)
    {
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery { Page = invalidPage });

        result.ShouldHaveValidationErrorFor(query => query.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_WhenPageSizeIsLessThanOne_HasValidationError(int invalidPageSize)
    {
        var result = _validator.TestValidate(new GetEmployerCompanyReviewsQuery { PageSize = invalidPageSize });

        result.ShouldHaveValidationErrorFor(query => query.PageSize)
            .WithErrorMessage("PageSize must be at least 1.");
    }
}
