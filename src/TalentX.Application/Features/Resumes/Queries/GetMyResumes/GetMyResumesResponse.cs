using TalentX.Application.Features.Resumes.DTOs;

namespace TalentX.Application.Features.Resumes.Queries.GetMyResumes;

public sealed record GetMyResumesResponse(
    IReadOnlyList<SavedResumeDto> Resumes,
    int MaxResumes);
