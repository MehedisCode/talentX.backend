using TalentX.Application.Common.Models;
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
}
