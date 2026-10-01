using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;

public sealed record RejectCompanyReviewCommand(
    Guid ReviewId,
    ReviewModerationReason Reason,
    string Details
) : IRequest<StatusUpdateResponseDto>;
