using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Commands.ApproveCompanyReview;

public sealed class ApproveCompanyReviewCommandHandler : IRequestHandler<ApproveCompanyReviewCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ApproveCompanyReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(ApproveCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId
            ?? throw new UnauthorizedException("You must be signed in to approve a review.");
        var actorEmail = _currentUserService.Email
            ?? throw new UnauthorizedException("You must be signed in to approve a review.");

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("CompanyReview", request.ReviewId);

        try
        {
            review.Approve(actorUserId);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var company = await _unitOfWork.Companies.GetByIdAsync(review.CompanyId, cancellationToken);

        // Mentions the company and job title + review id so the line is traceable without a
        // per-review moderation-history table; never the reviewer's name.
        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Approved review {review.Id} for '{company?.Name}' ({review.JobTitle})"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Review approved.");
    }
}
