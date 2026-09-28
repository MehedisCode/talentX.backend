using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.CompanyReviews.Commands.SubmitCompanyReview;

public sealed class SubmitCompanyReviewCommandHandler : IRequestHandler<SubmitCompanyReviewCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanyReviewSettings _settings;

    public SubmitCompanyReviewCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICompanyReviewSettings settings)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public async Task<Guid> Handle(SubmitCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var application = await _unitOfWork.JobApplications.GetByIdAsync(request.JobApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(JobApplication), request.JobApplicationId);

        if (application.TalentUserId != userId)
        {
            throw new ForbiddenException("You can only review a company for your own job applications.");
        }

        var existing = await _unitOfWork.CompanyReviews.GetByJobApplicationIdAsync(application.Id, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("You have already reviewed the company for this application.");
        }

        var acceptedInvitations = await _unitOfWork.InterviewInvitations
            .GetAcceptedByApplicationIdAsync(application.Id, cancellationToken);
        if (acceptedInvitations.Count == 0)
        {
            throw new ForbiddenException("You can only review a company after an accepted interview.");
        }

        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(acceptedInvitations, _settings.AssessmentReviewDelayDays);
        if (!CompanyReviewRules.IsUnlocked(unlockAt, DateTime.UtcNow))
        {
            throw new UnprocessableEntityException(unlockAt is null
                ? "This interview has no schedule yet, so the company can't be reviewed."
                : $"You can review this company after your interview ends, from {unlockAt.Value:MMM d, yyyy HH:mm} UTC.");
        }

        var job = await _unitOfWork.Jobs.GetByIdIncludingDeletedAsync(application.JobId, cancellationToken)
            ?? throw new NotFoundException(nameof(Job), application.JobId);

        var company = await _unitOfWork.Companies.GetByEmployerIdAsync(job.EmployerId, cancellationToken);
        if (company is null)
        {
            company = new Company(job.EmployerId, job.CompanyName);
            await _unitOfWork.Companies.AddAsync(company, cancellationToken);
        }

        CompanyReviewRules.EnsureRatingsInRange(
            request.Ratings?.RecruitmentProcess,
            request.Ratings?.Communication,
            request.Ratings?.InterviewExperience,
            request.Ratings?.Professionalism);

        var review = CompanyReview.Create(
            company.Id,
            job.Id,
            application.Id,
            userId,
            job.Title,
            request.Sentiment,
            CompanyReviewRules.NormalizeComment(request.Comment),
            request.Ratings?.RecruitmentProcess,
            request.Ratings?.Communication,
            request.Ratings?.InterviewExperience,
            request.Ratings?.Professionalism);

        await _unitOfWork.CompanyReviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return review.Id;
    }
}
