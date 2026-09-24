using MediatR;

namespace TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed record WithdrawCompanyReviewCommand(Guid ReviewId) : IRequest;
