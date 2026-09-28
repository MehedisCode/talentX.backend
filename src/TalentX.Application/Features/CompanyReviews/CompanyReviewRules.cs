using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews;

// this is only used to check for eligibility of review 
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
}
