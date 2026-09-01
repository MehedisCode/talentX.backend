namespace TalentX.Application.Features.JobApplications.DTOs;

// response dto for get job application details
public sealed record JobApplicationDetailsDto(
    Guid Id,
    Guid JobId,
    string ApplicantName,
    string ApplicantEmail,
    string ResumeStorageKey,
    string? ResumeDownloadUrl,
    string? CoverLetter,
    decimal? ExpectedSalary,
    decimal? TotalYearsExperience,
    string? Location,
    string Status,
    DateTime AppliedAt,
    DateTime? UpdatedAt);
