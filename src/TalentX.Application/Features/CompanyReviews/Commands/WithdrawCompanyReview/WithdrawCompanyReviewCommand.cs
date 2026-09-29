using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed record WithdrawCompanyReviewCommand(Guid ReviewId) : IRequest<StatusUpdateResponseDto>;
