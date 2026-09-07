using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class InterviewInvitation : BaseEntity
{
    public Guid JobId { get; private set; }
    public Guid JobApplicationId { get; private set; }
    public string EmployerUserId { get; private set; } = string.Empty;
    public string TalentUserId { get; private set; } = string.Empty;

    public InterviewType Type { get; private set; }
    public InterviewInvitationStatus Status { get; private set; }

    // OnlineAssessment specific
    public string? AssessmentUrl { get; private set; }
    public DateTime? AssessmentDeadline { get; private set; }

    // OnCall (Google Meet / Zoom) or OnSite (Office address)
    public string? LocationOrLink { get; private set; }

    public string? Instructions { get; private set; }
    public string? DeclineReason { get; private set; }

    // Selection details upon acceptance
    public Guid? SelectedTimeslotId { get; private set; }
    public DateTime? ScheduledAt { get; private set; }

    public DateTime SentAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public List<InterviewTimeslot> Timeslots { get; private set; } = new();

    private InterviewInvitation() { }

    public InterviewInvitation(
        Guid jobId,
        Guid jobApplicationId,
        string employerUserId,
        string talentUserId,
        InterviewType type,
        string? assessmentUrl = null,
        DateTime? assessmentDeadline = null,
        string? locationOrLink = null,
        string? instructions = null)
    {
        JobId = jobId;
        JobApplicationId = jobApplicationId;
        EmployerUserId = employerUserId;
        TalentUserId = talentUserId;
        Type = type;
        Status = InterviewInvitationStatus.Pending;
        AssessmentUrl = assessmentUrl;
        AssessmentDeadline = assessmentDeadline;
        LocationOrLink = locationOrLink;
        Instructions = instructions;
        SentAt = DateTime.UtcNow;
    }

    public void AddTimeslot(DateTime startTime, DateTime endTime, string? meetingUrl = null)
    {
        Timeslots.Add(new InterviewTimeslot(Id, startTime, endTime, meetingUrl));
    }

    public void Accept(Guid? selectedTimeslotId, DateTime? scheduledAt)
    {
        if (Status != InterviewInvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot accept invitation in {Status} status.");
        }

        Status = InterviewInvitationStatus.Accepted;
        SelectedTimeslotId = selectedTimeslotId;
        ScheduledAt = scheduledAt;
        RespondedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Decline(string? reason = null)
    {
        if (Status != InterviewInvitationStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot decline invitation in {Status} status.");
        }

        Status = InterviewInvitationStatus.Declined;
        DeclineReason = reason;
        RespondedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = InterviewInvitationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
