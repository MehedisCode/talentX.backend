using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;

namespace TalentX.Application.Features.CompanyReviews.Commands.UpdateCompanyReview;

public sealed class UpdateCompanyReviewCommandHandler : IRequestHandler<UpdateCompanyReviewCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCompanyReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to edit a review.");

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Company review", request.ReviewId);

        if (review.TalentUserId != userId)
        {
            throw new ForbiddenException("You can only edit your own reviews.");
        }

        try
        {
            review.Edit(
                request.Sentiment,
                CompanyReviewRules.NormalizeComment(request.Comment),
                request.Ratings?.RecruitmentProcess,
                request.Ratings?.Communication,
                request.Ratings?.InterviewExperience,
                request.Ratings?.Professionalism);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
