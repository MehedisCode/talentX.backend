using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.DTOs;

public sealed record SavedResumeDto(
    Guid Id,
    string DisplayName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    bool IsDefault,
    DateTime UploadedAt,
    ResumeExtractionResult? ExtractedProfile = null,
    bool AiServiceUnavailable = false)
{
    public static SavedResumeDto FromEntity(Resume resume) =>
        FromEntity(resume, null, false);

    public static SavedResumeDto FromEntity(
        Resume resume,
        ResumeExtractionResult? extractedProfile = null,
        bool aiServiceUnavailable = false) => new(
        resume.Id,
        resume.DisplayName,
        resume.OriginalFileName,
        resume.ContentType,
        resume.SizeBytes,
        resume.IsDefault,
        resume.UploadedAt,
        extractedProfile,
        aiServiceUnavailable);
}
