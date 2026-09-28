using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed class WithdrawCompanyReviewCommandHandler : IRequestHandler<WithdrawCompanyReviewCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public WithdrawCompanyReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(WithdrawCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Company review", request.ReviewId);

        if (review.TalentUserId != userId)
        {
            throw new ForbiddenException("You can only withdraw your own reviews.");
        }

        CompanyReviewRules.EnsureCanBeWithdrawn(review);
        review.Withdraw();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Review withdrawn.");
    }
}
