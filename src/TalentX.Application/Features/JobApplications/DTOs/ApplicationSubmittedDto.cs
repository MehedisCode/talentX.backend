namespace TalentX.Application.Features.JobApplications.DTOs;

// Return DTO for the POST /api/v1/jobs/{jobId}/applications response.
public sealed record ApplicationSubmittedDto(Guid Id, Guid JobId, string Status, DateTime AppliedAt);
