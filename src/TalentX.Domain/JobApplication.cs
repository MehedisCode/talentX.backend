using TalentX.Domain.Common;
using TalentX.Domain.Enums;

namespace TalentX.Domain.Entities;

public sealed class JobApplication : BaseEntity
{
    public Guid JobId { get; private set; }
    public string? TalentUserId { get; private set; }
    public string ApplicantName { get; private set; } = string.Empty;
    public string ApplicantEmail { get; private set; } = string.Empty;
    public string ResumeStorageKey { get; private set; } = string.Empty;
    public string? CoverLetter { get; private set; }
    public decimal? ExpectedSalary { get; private set; }
    public decimal? TotalYearsExperience { get; private set; }
    public string? Location { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public decimal? AiMatchScore { get; private set; }
    public DateTime AppliedAt { get; private set; }

    private JobApplication() { }

    public JobApplication(
        Guid jobId,
        string? talentUserId,
        string applicantName,
        string applicantEmail,
        string resumeStorageKey,
        string? coverLetter = null,
        decimal? expectedSalary = null,
        decimal? totalYearsExperience = null,
        string? location = null)
    {
        JobId = jobId;
        TalentUserId = talentUserId;
        ApplicantName = applicantName;
        ApplicantEmail = applicantEmail;
        ResumeStorageKey = resumeStorageKey;
        CoverLetter = coverLetter;
        ExpectedSalary = expectedSalary;
        TotalYearsExperience = totalYearsExperience;
        Location = location;
        Status = string.IsNullOrWhiteSpace(talentUserId) ? ApplicationStatus.PendingVerification : ApplicationStatus.Applied;
        AppliedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public static JobApplication CreateGuestApplication(
        Guid jobId,
        string applicantName,
        string applicantEmail,
        string resumeStorageKey,
        string? coverLetter = null,
        decimal? expectedSalary = null,
        decimal? totalYearsExperience = null,
        string? location = null)
    {
        return new JobApplication(
            jobId,
            talentUserId: null,
            applicantName,
            applicantEmail,
            resumeStorageKey,
            coverLetter,
            expectedSalary,
            totalYearsExperience,
            location);
    }

    public void UpdateStatus(ApplicationStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiMatchScore(decimal? score)
    {
        AiMatchScore = score;
    }

    public void LinkTalentUser(string talentUserId)
    {
        TalentUserId = talentUserId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string applicantName,
        string applicantEmail,
        string resumeStorageKey,
        string? coverLetter,
        decimal? expectedSalary,
        decimal? totalYearsExperience,
        string? location)
    {
        ApplicantName = applicantName;
        ApplicantEmail = applicantEmail;
        ResumeStorageKey = resumeStorageKey;
        CoverLetter = coverLetter;
        ExpectedSalary = expectedSalary;
        TotalYearsExperience = totalYearsExperience;
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }
}
