using MediatR;
using TalentX.Application.Features.JobApplications.DTOs;

namespace TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;

public sealed record UnhideCompanyReviewCommand(Guid ReviewId) : IRequest<StatusUpdateResponseDto>;
