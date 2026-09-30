namespace TalentX.Application.Features.JobApplications.DTOs;

using TalentX.Application.Features.Employers.DTOs;

public sealed record JobPipelineSummaryDto(
    Guid JobId,
    string JobTitle,
    string CompanyName,
    string JobStatus,
    int Vacancies,
    DateTime PostedDate,
    int TotalApplicants,
    CandidatePipelineStatsDto PipelineStats);
