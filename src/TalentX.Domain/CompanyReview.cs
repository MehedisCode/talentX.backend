using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class CompanyReview : BaseEntity
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    public Guid CompanyId { get; private set; }
    // Plain snapshot (no FK): reviews stay with the company even after the job is closed, expired or deleted.
    public Guid JobId { get; private set; }
    public Guid JobApplicationId { get; private set; }
    public string TalentUserId { get; private set; } = string.Empty;
    public string JobTitle { get; private set; } = string.Empty;
    public ReviewSentiment Sentiment { get; private set; }
    public string? Comment { get; private set; }
    public int? RecruitmentProcessRating { get; private set; }
    public int? CommunicationRating { get; private set; }
    public int? InterviewExperienceRating { get; private set; }
    public int? ProfessionalismRating { get; private set; }
    public CompanyReviewStatus Status { get; private set; }
    public ReviewModerationReason? ModerationReason { get; private set; }
    public string? ModerationDetails { get; private set; }
    public string? ModeratedByUserId { get; private set; }
    public DateTime? ModeratedAt { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }

    // Pending/Published/Rejected reviews can be edited or withdrawn by the talent.
    // Hidden is a moderator takedown and Withdrawn is final, so neither can be changed.
    public bool CanBeChangedByTalent =>
        Status is CompanyReviewStatus.Pending or CompanyReviewStatus.Published or CompanyReviewStatus.Rejected;

    private CompanyReview()
    {
    }

    public static CompanyReview Create(
        Guid companyId,
        Guid jobId,
        Guid jobApplicationId,
        string talentUserId,
        string jobTitle,
        ReviewSentiment sentiment,
        string? comment,
        int? recruitmentProcessRating,
        int? communicationRating,
        int? interviewExperienceRating,
        int? professionalismRating
    )
    {
        EnsureRatingsInRange(
            recruitmentProcessRating,
            communicationRating,
            interviewExperienceRating,
            professionalismRating);

        var now = DateTime.UtcNow;

        return new CompanyReview
        {
            CompanyId = companyId,
            JobId = jobId,
            JobApplicationId = jobApplicationId,
            TalentUserId = talentUserId,
            JobTitle = jobTitle,
            Sentiment = sentiment,
            Comment = comment,
            RecruitmentProcessRating = recruitmentProcessRating,
            CommunicationRating = communicationRating,
            InterviewExperienceRating = interviewExperienceRating,
            ProfessionalismRating = professionalismRating,
            Status = CompanyReviewStatus.Pending,
            SubmittedAt = now,
            CreatedAt = now
        };
    }

    /// <summary>
    /// Talent edit. Always re-queues the review for moderation, so a published review
    /// leaves the public view until an admin approves the new text.
    /// </summary>
    public void Edit(
        ReviewSentiment sentiment,
        string? comment,
        int? recruitmentProcessRating,
        int? communicationRating,
        int? interviewExperienceRating,
        int? professionalismRating
    )
    {
        if (!CanBeChangedByTalent)
        {
            throw new InvalidOperationException(Status switch
            {
                CompanyReviewStatus.Hidden => "This review was hidden by a moderator and can no longer be edited.",
                _ => "A withdrawn review can no longer be edited."
            });
        }

        EnsureRatingsInRange(
            recruitmentProcessRating,
            communicationRating,
            interviewExperienceRating,
            professionalismRating);

        Sentiment = sentiment;
        Comment = comment;
        RecruitmentProcessRating = recruitmentProcessRating;
        CommunicationRating = communicationRating;
        InterviewExperienceRating = interviewExperienceRating;
        ProfessionalismRating = professionalismRating;
        Status = CompanyReviewStatus.Pending;
        PublishedAt = null;
        ClearModeration();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Talent withdrawal. Final: the talent can never review that application again.</summary>
    public void Withdraw()
    {
        if (!CanBeChangedByTalent)
        {
            throw new InvalidOperationException(Status switch
            {
                CompanyReviewStatus.Hidden => "This review was hidden by a moderator and can no longer be withdrawn.",
                _ => "This review has already been withdrawn."
            });
        }

        Status = CompanyReviewStatus.Withdrawn;
        WithdrawnAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(string actorUserId)
    {
        if (Status != CompanyReviewStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reviews can be approved.");
        }

        var now = DateTime.UtcNow;
        Status = CompanyReviewStatus.Published;
        PublishedAt = now;
        ModerationReason = null;
        ModerationDetails = null;
        ModeratedByUserId = actorUserId;
        ModeratedAt = now;
        UpdatedAt = now;
    }

    public void Reject(ReviewModerationReason reason, string details, string actorUserId)
    {
        if (Status != CompanyReviewStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reviews can be rejected.");
        }

        var now = DateTime.UtcNow;
        Status = CompanyReviewStatus.Rejected;
        ModerationReason = reason;
        ModerationDetails = details;
        ModeratedByUserId = actorUserId;
        ModeratedAt = now;
        UpdatedAt = now;
    }

    public void Hide(ReviewModerationReason reason, string details, string actorUserId)
    {
        if (Status != CompanyReviewStatus.Published)
        {
            throw new InvalidOperationException("Only published reviews can be hidden.");
        }

        var now = DateTime.UtcNow;
        Status = CompanyReviewStatus.Hidden;
        ModerationReason = reason;
        ModerationDetails = details;
        ModeratedByUserId = actorUserId;
        ModeratedAt = now;
        UpdatedAt = now;
    }

    public void Unhide(string actorUserId)
    {
        if (Status != CompanyReviewStatus.Hidden)
        {
            throw new InvalidOperationException("Only hidden reviews can be unhidden.");
        }

        var now = DateTime.UtcNow;
        Status = CompanyReviewStatus.Published;
        ModerationReason = null;
        ModerationDetails = null;
        ModeratedByUserId = actorUserId;
        ModeratedAt = now;
        UpdatedAt = now;
    }

    private void ClearModeration()
    {
        ModerationReason = null;
        ModerationDetails = null;
        ModeratedByUserId = null;
        ModeratedAt = null;
    }

    // Defense in depth — the command validators give the user-facing message.
    private static void EnsureRatingsInRange(
        int? recruitmentProcessRating,
        int? communicationRating,
        int? interviewExperienceRating,
        int? professionalismRating)
    {
        EnsureInRange(recruitmentProcessRating, nameof(RecruitmentProcessRating));
        EnsureInRange(communicationRating, nameof(CommunicationRating));
        EnsureInRange(interviewExperienceRating, nameof(InterviewExperienceRating));
        EnsureInRange(professionalismRating, nameof(ProfessionalismRating));
    }

    private static void EnsureInRange(int? rating, string paramName)
    {
        if (rating is < MinRating or > MaxRating)
        {
            throw new ArgumentOutOfRangeException(paramName, rating, $"A rating must be between {MinRating} and {MaxRating}.");
        }
    }
}
