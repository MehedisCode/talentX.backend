using TalentX.Domain.Common;

namespace TalentX.Domain.Entities;

public sealed class InterviewTimeslot : BaseEntity
{
    public Guid InterviewInvitationId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public string? MeetingUrl { get; private set; }
    public bool IsBooked { get; private set; }
    public string? BookedByTalentUserId { get; private set; }

    private InterviewTimeslot() { }

    public InterviewTimeslot(
        Guid interviewInvitationId,
        DateTime startTime,
        DateTime endTime,
        string? meetingUrl = null)
    {
        InterviewInvitationId = interviewInvitationId;
        StartTime = startTime;
        EndTime = endTime;
        MeetingUrl = meetingUrl;
        IsBooked = false;
    }

    public void Book(string talentUserId)
    {
        IsBooked = true;
        BookedByTalentUserId = talentUserId;
        UpdatedAt = DateTime.UtcNow;
    }
}
