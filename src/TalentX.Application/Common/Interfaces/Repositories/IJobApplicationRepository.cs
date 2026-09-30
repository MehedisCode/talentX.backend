using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;

namespace TalentX.Application.Common.Interfaces.Repositories;

public sealed record TalentApplicationRecord(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string Company,
    DateTime AppliedDate,
    ApplicationStatus Status);

public interface IJobApplicationRepository
{
    Task<bool> ExistsByJobIdAndTalentUserIdAsync(Guid jobId, string talentUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingByJobIdAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetPendingByJobIdAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);
    Task<JobApplication?> GetAnyByJobIdAndEmailAsync(Guid jobId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplication>> GetPendingByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobApplication>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<PaginatedList<JobApplication>> GetByJobIdPaginatedAsync(
        Guid jobId,
        string? search,
        ApplicationStatus? status,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    void Update(JobApplication jobApplication);
    void Delete(JobApplication jobApplication);
    Task<JobApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(JobApplication jobApplication, CancellationToken cancellationToken = default);
    Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default);
    Task<PaginatedList<TalentApplicationRecord>> GetApplicationsForTalentAsync(
        string talentUserId,
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountHiredByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);

    // Returns how many applications were submitted in the range, grouped by status.  PendingVerification (unverified guest
    // applications) is excluded from the application counts.
    Task<IReadOnlyDictionary<ApplicationStatus, int>> GetStatusCountsAppliedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<DateTime, int>> GetAppliedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default);

    // Returns how many candidates were hired in the range per UTC hour (groupByHour) or UTC day. There is no HiredAt
    // column, so a hire is dated by UpdatedAt on applications whose status is Hired.
    Task<IReadOnlyDictionary<DateTime, int>> GetHiredCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default);
    Task<CandidatePipelineStatsDto> GetCandidatePipelineStatsByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default);
}
