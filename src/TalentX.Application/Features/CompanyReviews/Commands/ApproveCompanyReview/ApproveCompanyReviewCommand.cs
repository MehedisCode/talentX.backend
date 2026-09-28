using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;

public sealed record ApproveCompanyReviewCommand(Guid ReviewId) : IRequest<StatusUpdateResponseDto>;
