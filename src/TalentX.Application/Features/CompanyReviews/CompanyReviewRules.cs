using TalentX.Application.Common.Exceptions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews;

public static class CompanyReviewRules
{
    public const int MaxCommentLength = 2000;

    public static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    public static DateTime? TryGetUnlockAt(InterviewInvitation invitation, int assessmentDelayDays)
    {
        if (invitation.Status != InterviewInvitationStatus.Accepted)
            return null;

        return invitation.Type switch
        {
            InterviewType.OnlineAssessment => invitation.AssessmentDeadline?.AddDays(assessmentDelayDays),
            InterviewType.OnCall or InterviewType.OnSite => invitation.Timeslots
                .FirstOrDefault(slot => slot.Id == invitation.SelectedTimeslotId)?.EndTime,
            _ => null
        };
    }

    public static DateTime? TryGetApplicationUnlockAt(
        IEnumerable<InterviewInvitation> invitationsOfOneApplication,
        int assessmentDelayDays) =>
        invitationsOfOneApplication
            .Select(invitation => TryGetUnlockAt(invitation, assessmentDelayDays))
            .Where(unlockAt => unlockAt.HasValue)
            .Min();
    public static bool IsUnlocked(DateTime? unlockAt, DateTime utcNow) =>
        unlockAt.HasValue && unlockAt.Value <= utcNow;

    public static bool CanBeChangedByTalent(CompanyReview review) =>
        review.Status is CompanyReviewStatus.Pending or CompanyReviewStatus.Published or CompanyReviewStatus.Rejected;

    public static void EnsureCanApprove(CompanyReview review)
    {
        if (review.Status != CompanyReviewStatus.Pending)
        {
            throw new BadRequestException("Only pending reviews can be approved.");
        }
    }

    public static void EnsureCanReject(CompanyReview review)
    {
        if (review.Status != CompanyReviewStatus.Pending)
        {
            throw new BadRequestException("Only pending reviews can be rejected.");
        }
    }

    public static void EnsureCanHide(CompanyReview review)
    {
        if (review.Status != CompanyReviewStatus.Published)
        {
            throw new BadRequestException("Only published reviews can be hidden.");
        }
    }

    public static void EnsureCanUnhide(CompanyReview review)
    {
        if (review.Status != CompanyReviewStatus.Hidden)
        {
            throw new BadRequestException("Only hidden reviews can be unhidden.");
        }
    }

    public static void EnsureCanBeEdited(CompanyReview review)
    {
        if (!CanBeChangedByTalent(review))
        {
            throw new BadRequestException(review.Status == CompanyReviewStatus.Hidden
                ? "This review was hidden by a moderator and can no longer be edited."
                : "A withdrawn review can no longer be edited.");
        }
    }

    public static void EnsureCanBeWithdrawn(CompanyReview review)
    {
        if (!CanBeChangedByTalent(review))
        {
            throw new BadRequestException(review.Status == CompanyReviewStatus.Hidden
                ? "This review was hidden by a moderator and can no longer be withdrawn."
                : "This review has already been withdrawn.");
        }
    }

    public static void EnsureRatingsInRange(
        int? recruitmentProcessRating,
        int? communicationRating,
        int? interviewExperienceRating,
        int? professionalismRating)
    {
        EnsureRatingInRange(recruitmentProcessRating);
        EnsureRatingInRange(communicationRating);
        EnsureRatingInRange(interviewExperienceRating);
        EnsureRatingInRange(professionalismRating);
    }

    private static void EnsureRatingInRange(int? rating)
    {
        if (rating is < CompanyReview.MinRating or > CompanyReview.MaxRating)
        {
            throw new BadRequestException($"A rating must be between {CompanyReview.MinRating} and {CompanyReview.MaxRating}.");
        }
    }
}
