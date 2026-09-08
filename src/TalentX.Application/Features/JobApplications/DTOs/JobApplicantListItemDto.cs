namespace TalentX.Application.Features.JobApplications.DTOs;

public sealed record JobApplicantListItemDto(
    Guid Id,
    Guid JobId,
    string TalentUserId,
    string ApplicantName,
    string ApplicantEmail,
    string? ResumeDownloadUrl,
    string Status,
    decimal? AiMatchScore,
    decimal? ExpectedSalary,
    decimal? TotalYearsExperience,
    string? Location,
    DateTime AppliedAt
);
