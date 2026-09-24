using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.UpdateCompanyReview;

public sealed class UpdateCompanyReviewCommandValidatorTests
{
    private readonly UpdateCompanyReviewCommandValidator _validator = new();

    private static UpdateCompanyReviewCommand ValidCommand(
        Guid? reviewId = null,
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        string? comment = "Great process.",
        CompanyReviewRatingsDto? ratings = null) =>
        new(reviewId ?? Guid.NewGuid(), sentiment, comment, ratings);

    [Fact]
    public void Validate_WithValidCommand_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand(ratings: new CompanyReviewRatingsDto(1, null, 3, 5)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenReviewIdIsEmpty_HasError()
    {
        _validator.TestValidate(ValidCommand(reviewId: Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.ReviewId)
            .WithErrorMessage("Review ID is required.");
    }

    [Fact]
    public void Validate_WhenSentimentIsNotADefinedValue_HasError()
    {
        _validator.TestValidate(ValidCommand(sentiment: (ReviewSentiment)42))
            .ShouldHaveValidationErrorFor(c => c.Sentiment);
    }

    [Fact]
    public void Validate_CommentOverTheMaximumLength_HasError()
    {
        _validator.TestValidate(ValidCommand(comment: new string('a', 2001)))
            .ShouldHaveValidationErrorFor(c => c.Comment)
            .WithErrorMessage("Feedback must not exceed 2000 characters.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RatingOutOfRange_HasError(int rating)
    {
        _validator.TestValidate(ValidCommand(ratings: new CompanyReviewRatingsDto(null, rating, null, null)))
            .ShouldHaveValidationErrorFor("Ratings.Communication")
            .WithErrorMessage("Communication rating must be between 1 and 5.");
    }
}
