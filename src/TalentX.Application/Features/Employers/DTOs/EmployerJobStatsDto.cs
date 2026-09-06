namespace TalentX.Application.Features.Employers.DTOs;

public sealed record EmployerJobStatsDto(
    int TotalPostedJobs,
    int ActiveJobsCount,
    int DraftJobsCount,
    int ClosedJobsCount,
    int ExpiredJobsCount);
