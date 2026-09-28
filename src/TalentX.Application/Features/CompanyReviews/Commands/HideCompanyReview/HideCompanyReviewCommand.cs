using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;

public sealed record HideCompanyReviewCommand(
    Guid ReviewId,
    ReviewModerationReason Reason,
    string Details
) : IRequest<StatusUpdateResponseDto>;
