namespace TalentX.Application.Features.Resumes.Queries.GetResumeUrl;

public sealed record GetResumeUrlResponse(
    Guid Id,
    string OriginalFileName,
    string DisplayName,
    string ContentType,
    string Url);
