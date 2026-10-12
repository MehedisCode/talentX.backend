using System.Text.Json;
using TalentX.Domain;
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
    public string? AiScoreInputHash { get; private set; }
    public DateTime AppliedAt { get; private set; }
    public string? ExtractedTechnicalSkillsJson { get; private set; }
    public string? ExtractedSoftSkillsJson { get; private set; }
    public string? ExtractedToolsAndPlatformsJson { get; private set; }
    public string? ExtractedWorkExperienceJson { get; private set; }
    public string? ExtractedEducationJson { get; private set; }
    public string? ExtractedProjectsJson { get; private set; }

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
        string? location = null,
        string? extractedTechnicalSkillsJson = null,
        string? extractedSoftSkillsJson = null,
        string? extractedToolsAndPlatformsJson = null,
        string? extractedWorkExperienceJson = null,
        string? extractedEducationJson = null,
        string? extractedProjectsJson = null)
    {
        var application = new JobApplication(
            jobId,
            talentUserId: null,
            applicantName,
            applicantEmail,
            resumeStorageKey,
            coverLetter,
            expectedSalary,
            totalYearsExperience,
            location);

        application.ExtractedTechnicalSkillsJson = extractedTechnicalSkillsJson;
        application.ExtractedSoftSkillsJson = extractedSoftSkillsJson;
        application.ExtractedToolsAndPlatformsJson = extractedToolsAndPlatformsJson;
        application.ExtractedWorkExperienceJson = extractedWorkExperienceJson;
        application.ExtractedEducationJson = extractedEducationJson;
        application.ExtractedProjectsJson = extractedProjectsJson;

        return application;
    }

    public List<string> GetExtractedTechnicalSkills() =>
        DeserializeStringList(ExtractedTechnicalSkillsJson);

    public List<string> GetExtractedSoftSkills() =>
        DeserializeStringList(ExtractedSoftSkillsJson);

    public List<string> GetExtractedToolsAndPlatforms() =>
        DeserializeStringList(ExtractedToolsAndPlatformsJson);

    public List<ExtractedWorkExperienceItem> GetExtractedWorkExperience() =>
        DeserializeList<ExtractedWorkExperienceItem>(ExtractedWorkExperienceJson);

    public List<ExtractedEducationItem> GetExtractedEducation() =>
        DeserializeList<ExtractedEducationItem>(ExtractedEducationJson);

    public List<ExtractedProjectItem> GetExtractedProjects() =>
        DeserializeList<ExtractedProjectItem>(ExtractedProjectsJson);

    public void UpdateStatus(ApplicationStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAiMatchScore(decimal? score, string? inputHash = null)
    {
        AiMatchScore = score;
        AiScoreInputHash = inputHash;
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

    private static List<string> DeserializeStringList(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(json, s_jsonOptions) ?? new List<string>();

    private static List<T> DeserializeList<T>(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new List<T>()
            : JsonSerializer.Deserialize<List<T>>(json, s_jsonOptions) ?? new List<T>();
}

public sealed class ExtractedWorkExperienceItem
{
    public string? CompanyName { get; init; }
    public string? JobTitle { get; init; }
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class ExtractedEducationItem
{
    public string? Institution { get; init; }
    public string? Degree { get; init; }
    public string? StartDate { get; init; }
    public string? EndDate { get; init; }
}
