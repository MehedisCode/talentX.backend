using FluentValidation;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews;

// Shared by the submit and update validators so both enforce the same rating range.
public sealed class CompanyReviewRatingsDtoValidator : AbstractValidator<CompanyReviewRatingsDto>
{
    public CompanyReviewRatingsDtoValidator()
    {
        RuleFor(r => r.RecruitmentProcess).Must(BeAValidRating)
            .WithMessage("Recruitment process rating must be between 1 and 5.");

        RuleFor(r => r.Communication).Must(BeAValidRating)
            .WithMessage("Communication rating must be between 1 and 5.");

        RuleFor(r => r.InterviewExperience).Must(BeAValidRating)
            .WithMessage("Interview experience rating must be between 1 and 5.");

        RuleFor(r => r.Professionalism).Must(BeAValidRating)
            .WithMessage("Professionalism rating must be between 1 and 5.");
    }

    private static bool BeAValidRating(int? rating) =>
        rating is null or (>= CompanyReview.MinRating and <= CompanyReview.MaxRating);
}
