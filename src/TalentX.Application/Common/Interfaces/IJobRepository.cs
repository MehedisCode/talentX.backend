using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);

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
    Task<PaginatedList<EmployerJobListItemDto>> GetEmployerJobsPaginatedAsync(string employerId, JobStatus? status, bool? isExpired, int page, int pageSize, CancellationToken cancellationToken = default);
}
