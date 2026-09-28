using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Features.JobApplications.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Commands.UnhideCompanyReview;

public sealed class UnhideCompanyReviewCommandHandler : IRequestHandler<UnhideCompanyReviewCommand, StatusUpdateResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnhideCompanyReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<StatusUpdateResponseDto> Handle(UnhideCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUserService.UserId!;
        var actorEmail = _currentUserService.Email!;

        var review = await _unitOfWork.CompanyReviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("CompanyReview", request.ReviewId);

        try
        {
            review.Unhide(actorUserId);
        }
        catch (InvalidOperationException exception)
        {
            throw new BadRequestException(exception.Message);
        }

        var company = await _unitOfWork.Companies.GetByIdAsync(review.CompanyId, cancellationToken);

        await _unitOfWork.AuditLogs.AddAsync(
            new AuditLog(actorEmail, $"Unhid review {review.Id} for '{company?.Name}' ({review.JobTitle})"),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StatusUpdateResponseDto("Review unhidden.");
    }
}
