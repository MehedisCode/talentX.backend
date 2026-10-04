using TalentX.Domain.Entities;

namespace TalentX.Application.Features.Resumes.DTOs;

public sealed record SavedResumeDto(
    Guid Id,
    string DisplayName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    bool IsDefault,
    DateTime UploadedAt)
{
    public static SavedResumeDto FromEntity(Resume resume) => new(
        resume.Id,
        resume.DisplayName,
        resume.OriginalFileName,
        resume.ContentType,
        resume.SizeBytes,
        resume.IsDefault,
        resume.UploadedAt);
}
