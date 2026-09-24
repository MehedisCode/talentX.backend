using MediatR;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;

public sealed record UpdateCompanyReviewCommand(
    Guid ReviewId,
    ReviewSentiment Sentiment,
    string? Comment,
    CompanyReviewRatingsDto? Ratings
) : IRequest;
