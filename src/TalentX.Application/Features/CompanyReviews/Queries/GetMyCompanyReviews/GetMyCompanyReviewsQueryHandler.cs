using MediatR;
using TalentX.Application.Common.Exceptions;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.CompanyReviews.DTOs;
using TalentX.Application.Identity.Services;
using TalentX.Domain;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews.Queries.GetMyCompanyReviews;

public sealed class GetMyCompanyReviewsQueryHandler
    : IRequestHandler<GetMyCompanyReviewsQuery, PaginatedList<MyCompanyReviewItemDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICompanyReviewSettings _settings;

    public GetMyCompanyReviewsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICompanyReviewSettings settings)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _settings = settings;
    }

    public async Task<PaginatedList<MyCompanyReviewItemDto>> Handle(
        GetMyCompanyReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!;

        var (page, pageSize) = PaginationRequest.Normalize(request.Page, request.PageSize);

        var accepted = await _unitOfWork.InterviewInvitations
            .GetAcceptedByTalentUserIdAsync(userId, cancellationToken);

        var applications = accepted
            .GroupBy(invitation => invitation.JobApplicationId)
            .ToList();

        var reviews = (await _unitOfWork.CompanyReviews.GetByJobApplicationIdsAsync(
                applications.Select(group => group.Key), cancellationToken))
            .ToDictionary(review => review.JobApplicationId);

        var employerIds = applications
            .Select(group => group.First().Job?.EmployerId)
            .OfType<string>()
            .Distinct()
            .ToList();
        var companiesByEmployerId = (await _unitOfWork.Companies.GetByEmployerIdsAsync(employerIds, cancellationToken))
            .ToDictionary(company => company.EmployerId);

        var now = DateTime.UtcNow;
        var items = applications
            .Select(group =>
            {
                reviews.TryGetValue(group.Key, out var review);
                return ToItem(group.ToList(), review, companiesByEmployerId, now);
            })
            .OrderByDescending(item => item.ReviewUnlocksAt.HasValue)
            .ThenByDescending(item => item.ReviewUnlocksAt)
            .ToList();

        var pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedList<MyCompanyReviewItemDto>(pageItems, page, pageSize, items.Count);
    }

    private MyCompanyReviewItemDto ToItem(
        IReadOnlyList<InterviewInvitation> rounds,
        CompanyReview? review,
        IReadOnlyDictionary<string, Company> companiesByEmployerId,
        DateTime now)
    {
        var delayDays = _settings.AssessmentReviewDelayDays;
        var unlockAt = CompanyReviewRules.TryGetApplicationUnlockAt(rounds, delayDays);

        var shownRound = rounds
            .Where(round => CompanyReviewRules.TryGetUnlockAt(round, delayDays) == unlockAt && unlockAt.HasValue)
            .FirstOrDefault()
            ?? rounds.OrderBy(round => round.SentAt).First();

        var job = shownRound.Job;
        Company? company = null;
        if (job is not null)
        {
            companiesByEmployerId.TryGetValue(job.EmployerId, out company);
        }

        return new MyCompanyReviewItemDto(
            shownRound.JobApplicationId,
            review?.JobTitle ?? job?.Title ?? "Job Position",
            review?.CompanyId ?? company?.Id,
            company?.Name ?? job?.CompanyName ?? "Company",
            shownRound.Type,
            unlockAt,
            review is null && CompanyReviewRules.IsUnlocked(unlockAt, now),
            review is null ? null : ToReviewDto(review),
            review?.CanBeChangedByTalent ?? false,
            review?.CanBeChangedByTalent ?? false);
    }

    private static MyCompanyReviewDto ToReviewDto(CompanyReview review)
    {
        var showModeration = review.Status is CompanyReviewStatus.Rejected or CompanyReviewStatus.Hidden;

        return new MyCompanyReviewDto(
            review.Id,
            review.Status,
            review.Sentiment,
            review.Comment,
            new CompanyReviewRatingsDto(
                review.RecruitmentProcessRating,
                review.CommunicationRating,
                review.InterviewExperienceRating,
                review.ProfessionalismRating),
            showModeration ? review.ModerationReason : null,
            showModeration ? review.ModerationDetails : null,
            review.SubmittedAt,
            review.UpdatedAt,
            review.PublishedAt);
    }
}
