using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Job?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Job>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);
    void RemoveRange(IEnumerable<Job> jobs);

    Task<PaginatedList<Job>> GetPublishedJobsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? keyword = null,
        string? location = null,
        EmploymentType? employmentType = null,
        ExperienceLevel? experienceLevel = null,
        WorkArrangement? workArrangement = null);

    Task<Job?> GetPublishedJobByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Job>> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);
    Task<List<Job>> GetByEmployerIdIncludingDeletedAsync(string employerId, CancellationToken cancellationToken = default);
    Task<List<Job>> GetActivePublishedJobsByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Job>> GetActivePublishedJobsAsync(CancellationToken cancellationToken);

    Task<EmployerJobStatsDto> GetEmployerStatsAsync(string employerId, CancellationToken cancellationToken = default);
    Task<List<EmployerRecentJobDto>> GetRecentJobsByEmployerIdAsync(
        string employerId,
        int count = EmployerDashboardConstants.RecentJobsLimit,
        CancellationToken cancellationToken = default);
    Task<PaginatedList<EmployerJobListItemDto>> GetEmployerJobsPaginatedAsync(
        string employerId,
        JobStatus? status,
        bool? isExpired,
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<AdminJobListItemDto>> GetAdminJobsPagedAsync(
        AdminJobQueryParams queryParams,
        CancellationToken cancellationToken = default);

    // Status counts of the jobs first posted in the window (reopens and reactivations are not new postings).
    Task<IReadOnlyDictionary<JobStatus, int>> GetStatusCountsFirstPublishedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    // Jobs first posted in the window, counted per hour or day bucket.
    Task<IReadOnlyDictionary<DateTime, int>> GetPublishedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default);

    // The newest jobs first posted in the window.
    Task<IReadOnlyList<Job>> GetRecentPublishedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        int take,
        CancellationToken cancellationToken = default);
}
