using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Commands.RejectCompanyReview;

public sealed class RejectCompanyReviewCommandHandler : IRequestHandler<RejectCompanyReviewCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RejectCompanyReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(RejectCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to reject a review.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to reject a review.");

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("CompanyReview", request.ReviewId);

        var details = request.Details.Trim();

        try
        {
            review.Reject(request.Reason, details, actorUserId);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var company = await _unitOfWork.Companies.GetByIdAsync(review.CompanyId, cancellationToken);
        var reasonLabel = ReviewModerationReasonLabels.ToLabel(request.Reason);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Rejected review {review.Id} for '{company?.Name}' ({review.JobTitle}) ({reasonLabel})"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Review rejected.");
    }
}
