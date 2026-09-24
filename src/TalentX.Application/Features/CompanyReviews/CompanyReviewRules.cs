using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.CompanyReviews;

// Shared by submit, edit and the talent's review list so "when can I review?"
// has exactly one definition.
public static class CompanyReviewRules
{
    public const int MaxCommentLength = 2000;

    public static string? NormalizeComment(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    // The moment a review of this interview unlocks, or null if it can't be derived
    // (not accepted, no booked slot, no deadline). Callers treat null as "not submittable".
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

    // Multi-round is deliberately not special-cased: the review unlocks as soon as the
    // EARLIEST accepted round's unlock time passes, and later rounds never re-lock it.
    public static DateTime? TryGetApplicationUnlockAt(
        IEnumerable<InterviewInvitation> invitationsOfOneApplication,
        int assessmentDelayDays) =>
        invitationsOfOneApplication
            .Select(invitation => TryGetUnlockAt(invitation, assessmentDelayDays))
            .Where(unlockAt => unlockAt.HasValue)
            .Min();

    // Unlocked from the exact unlock instant onward. Takes utcNow so tests are deterministic.
    public static bool IsUnlocked(DateTime? unlockAt, DateTime utcNow) =>
        unlockAt.HasValue && unlockAt.Value <= utcNow;
}
