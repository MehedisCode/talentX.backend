using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class CompanyReview : BaseEntity
{
    public const int MinRating = 1;
    public const int MaxRating = 5;

    public Guid CompanyId { get; private set; }
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

    public void Edit(
        ReviewSentiment sentiment,
        string? comment,
        int? recruitmentProcessRating,
        int? communicationRating,
        int? interviewExperienceRating,
        int? professionalismRating
    )
    {
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

    public void Withdraw()
    {
        Status = CompanyReviewStatus.Withdrawn;
        WithdrawnAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve(string actorUserId)
    {
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
}
