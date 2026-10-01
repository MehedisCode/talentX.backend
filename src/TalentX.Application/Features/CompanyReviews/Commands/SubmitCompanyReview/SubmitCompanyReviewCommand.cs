using MediatR;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;

public sealed record SubmitCompanyReviewCommand(
    Guid JobApplicationId,
    ReviewSentiment Sentiment,
    string? Comment,
    CompanyReviewRatingsDto? Ratings
) : IRequest<Guid>;
