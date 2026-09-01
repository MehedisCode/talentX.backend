namespace TalentX.Application.Features.JobApplications.DTOs;

public record UploadApplicationResumeResponse(
    string StorageKey,
    string OriginalFileName);
