using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.CompanyReviews.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetAdminCompanyReviewDetails;

public sealed class GetAdminCompanyReviewDetailsQueryHandler
    : IRequestHandler<GetAdminCompanyReviewDetailsQuery, AdminCompanyReviewDetailsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminCompanyReviewDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminCompanyReviewDetailsDto> Handle(GetAdminCompanyReviewDetailsQuery request, CancellationToken cancellationToken)
    {
        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("CompanyReview", request.ReviewId);

        var company = await _unitOfWork.Companies.GetByIdAsync(review.CompanyId, cancellationToken);
        var reviewer = await _unitOfWork.Users.GetByUserIdAsync(review.TalentUserId, cancellationToken);
        var moderator = review.ModeratedByUserId is null
            ? null
            : await _unitOfWork.Users.GetByUserIdAsync(review.ModeratedByUserId, cancellationToken);

        return new AdminCompanyReviewDetailsDto
        {
            Id = review.Id,
            Status = review.Status,
            Sentiment = review.Sentiment,
            Comment = review.Comment,
            Ratings = new CompanyReviewRatingsDto(
                review.RecruitmentProcessRating,
                review.CommunicationRating,
                review.InterviewExperienceRating,
                review.ProfessionalismRating),
            CompanyId = review.CompanyId,
            CompanyName = company?.Name ?? string.Empty,
            JobTitle = review.JobTitle,
            ReviewerName = reviewer?.Name ?? "Deleted user",
            ReviewerEmail = reviewer?.Email ?? string.Empty,
            ReviewerUserId = reviewer?.Id,
            SubmittedAt = review.SubmittedAt,
            UpdatedAt = review.UpdatedAt,
            PublishedAt = review.PublishedAt,
            ModerationReason = review.ModerationReason,
            ModerationDetails = review.ModerationDetails,
            ModeratedAt = review.ModeratedAt,
            ModeratedByEmail = moderator?.Email
        };
    }
}
