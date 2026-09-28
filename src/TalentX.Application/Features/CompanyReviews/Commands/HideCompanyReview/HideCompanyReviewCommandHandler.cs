using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Commands.HideCompanyReview;

public sealed class HideCompanyReviewCommandHandler : IRequestHandler<HideCompanyReviewCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public HideCompanyReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(HideCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId!;
        var actorEmail = _currentUserService.Email!;

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("CompanyReview", request.ReviewId);

        var details = request.Details.Trim();

        CompanyReviewRules.EnsureCanHide(review);
        review.Hide(request.Reason, details, actorUserId);

        var company = await _unitOfWork.Companies.GetByIdAsync(review.CompanyId, cancellationToken);
        var reasonLabel = ReviewModerationReasonLabels.ToLabel(request.Reason);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Hid review {review.Id} for '{company?.Name}' ({review.JobTitle}) ({reasonLabel})"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Review hidden.");
    }
}
