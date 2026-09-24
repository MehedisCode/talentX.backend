using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.CompanyReviews.Commands.WithdrawCompanyReview;

public sealed class WithdrawCompanyReviewCommandHandler : IRequestHandler<WithdrawCompanyReviewCommand>
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

    public async Task Handle(WithdrawCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to withdraw a review.");

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Company review", request.ReviewId);

        if (review.TalentUserId != userId)
        {
            throw new ForbiddenException("You can only withdraw your own reviews.");
        }

        try
        {
            review.Withdraw();
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
