using FluentAssertions;
using FluentValidation.TestHelper;
using TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;
using Xunit;

namespace TalentX.Application.Tests.Features.CompanyReviews.Commands.SubmitCompanyReview;

public sealed class SubmitCompanyReviewCommandValidatorTests
{
    private readonly SubmitCompanyReviewCommandValidator _validator = new();

    private static SubmitCompanyReviewCommand ValidCommand(
        Guid? applicationId = null,
        ReviewSentiment sentiment = ReviewSentiment.Positive,
        string? comment = "Great process.",
        CompanyReviewRatingsDto? ratings = null) =>
        new(applicationId ?? Guid.NewGuid(), sentiment, comment, ratings);

    [Fact]
    public void Validate_WithOnlyTheRequiredSentiment_HasNoErrors()
    {
        _validator.TestValidate(ValidCommand(comment: null, ratings: null)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEverythingFilledIn_HasNoErrors()
    {
        var command = ValidCommand(ratings: new CompanyReviewRatingsDto(1, 2, 3, 5));

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenApplicationIdIsEmpty_HasError()
    {
        _validator.TestValidate(ValidCommand(applicationId: Guid.Empty))
            .ShouldHaveValidationErrorFor(c => c.JobApplicationId)
            .WithErrorMessage("Job application ID is required.");
    }

    [Fact]
    public void Validate_WhenSentimentIsNotADefinedValue_HasError()
    {
        _validator.TestValidate(ValidCommand(sentiment: (ReviewSentiment)999))
            .ShouldHaveValidationErrorFor(c => c.Sentiment)
            .WithErrorMessage("Please choose Positive or Negative.");
    }

    [Fact]
    public void Validate_CommentOfExactlyTheMaximumLength_IsAllowed()
    {
        _validator.TestValidate(ValidCommand(comment: new string('a', 2000)))
            .ShouldNotHaveValidationErrorFor(c => c.Comment);
    }

    [Fact]
    public void Validate_CommentOverTheMaximumLength_HasError()
    {
        _validator.TestValidate(ValidCommand(comment: new string('a', 2001)))
            .ShouldHaveValidationErrorFor(c => c.Comment)
            .WithErrorMessage("Feedback must not exceed 2000 characters.");
    }

    [Fact]
    public void Validate_CommentThatIsOnlyOverTheLimitBecauseOfPadding_IsAllowed()
    {
        // The comment is trimmed before it is stored, so padding must not count.
        _validator.TestValidate(ValidCommand(comment: "  " + new string('a', 2000) + "  "))
            .ShouldNotHaveValidationErrorFor(c => c.Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Validate_RatingOutOfRange_HasErrorForEachArea(int rating)
    {
        var result = _validator.TestValidate(ValidCommand(ratings: new CompanyReviewRatingsDto(rating, rating, rating, rating)));

        result.ShouldHaveValidationErrorFor("Ratings.RecruitmentProcess")
            .WithErrorMessage("Recruitment process rating must be between 1 and 5.");
        result.ShouldHaveValidationErrorFor("Ratings.Communication")
            .WithErrorMessage("Communication rating must be between 1 and 5.");
        result.ShouldHaveValidationErrorFor("Ratings.InterviewExperience")
            .WithErrorMessage("Interview experience rating must be between 1 and 5.");
        result.ShouldHaveValidationErrorFor("Ratings.Professionalism")
            .WithErrorMessage("Professionalism rating must be between 1 and 5.");
    }

    [Fact]
    public void Validate_NullRatingsWithinRatingsObject_AreAllowed()
    {
        var command = ValidCommand(ratings: new CompanyReviewRatingsDto(null, null, null, null));

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
