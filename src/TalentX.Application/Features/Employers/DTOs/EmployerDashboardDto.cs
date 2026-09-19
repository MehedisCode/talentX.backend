namespace TalentX.Application.Features.Employers.DTOs;

public sealed record EmployerDashboardDto(
    int TotalPostedJobs,
    int TotalHires,
    int ActiveJobsCount,
    int DraftJobsCount,
    int ClosedJobsCount,
    int ExpiredJobsCount,
    int DeactivatedJobsCount,
    int PendingReviewJobsCount,
    IReadOnlyList<EmployerRecentJobDto> RecentJobs);
