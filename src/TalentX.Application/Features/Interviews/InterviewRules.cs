using TalentX.Application.Common.Exceptions;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Features.Interviews;

public static class InterviewRules
{
    public static void EnsureNotExpired(InterviewInvitation invitation, DateTime utcNow)
    {
        var expired = invitation.Type switch
        {
            InterviewType.OnlineAssessment => invitation.AssessmentDeadline.HasValue
                && invitation.AssessmentDeadline.Value <= utcNow,
            InterviewType.OnCall or InterviewType.OnSite => invitation.Timeslots.Count > 0
                && invitation.Timeslots.All(slot => slot.StartTime <= utcNow),
            _ => false
        };

        if (expired)
        {
            throw new BadRequestException("This interview invitation has expired.");
        }
    }

    public static void EnsureIsPending(InterviewInvitation invitation)
    {
        if (invitation.Status != InterviewInvitationStatus.Pending)
        {
            throw new ConflictException($"This interview invitation is no longer active (status: {invitation.Status}).");
        }
    }

    // Mirrors the talent portal's own isPast check (modules/talent-interviews/utils/interview.utils.ts)
    // so the "My Interviews" list order and the card's own expired/completed badge never disagree.
    public static bool IsPast(InterviewInvitation invitation, DateTime utcNow)
    {
        if (invitation.ScheduledAt.HasValue)
        {
            return invitation.ScheduledAt.Value < utcNow;
        }

        if (invitation.AssessmentDeadline.HasValue)
        {
            return invitation.AssessmentDeadline.Value < utcNow;
        }

        if (invitation.Timeslots.Count > 0)
        {
            return invitation.Timeslots.All(slot => slot.StartTime < utcNow);
        }

        return false;
    }

    // The single date used to both decide IsPast and to sort invitations against each other:
    // the scheduled time once accepted, else the assessment deadline, else the earliest offered
    // timeslot, else (no date at all yet) when the invite was sent.
    public static DateTime GetEffectiveAt(InterviewInvitation invitation)
    {
        if (invitation.ScheduledAt.HasValue)
        {
            return invitation.ScheduledAt.Value;
        }

        if (invitation.AssessmentDeadline.HasValue)
        {
            return invitation.AssessmentDeadline.Value;
        }

        if (invitation.Timeslots.Count > 0)
        {
            return invitation.Timeslots.Min(slot => slot.StartTime);
        }

        return invitation.SentAt;
    }

    // Sort priority for a talent's "My Interviews" list: 0 = still upcoming/actionable,
    // 1 = ended (accepted-but-over or never-answered) or cancelled, 2 = declined by the talent.
    public static int GetTalentListSortBucket(InterviewInvitation invitation, DateTime utcNow)
    {
        if (invitation.Status == InterviewInvitationStatus.Declined)
        {
            return 2;
        }

        if (invitation.Status == InterviewInvitationStatus.Cancelled)
        {
            return 1;
        }

        return IsPast(invitation, utcNow) ? 1 : 0;
    }
}
