using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobApplicationRepository : IJobApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public JobApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByJobIdAndTalentUserIdAsync(
        Guid jobId,
        string talentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AnyAsync(
                application => application.JobId == jobId && application.TalentUserId == talentUserId,
                cancellationToken);
    }

    public async Task<bool> ExistsByResumeStorageKeyAsync(
        string resumeStorageKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AnyAsync(
                application => application.ResumeStorageKey == resumeStorageKey,
                cancellationToken);
    }

    public async Task<int> CountByResumeStorageKeyAsync(
        string resumeStorageKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .CountAsync(
                application => application.ResumeStorageKey == resumeStorageKey,
                cancellationToken);
    }

    public async Task<bool> ExistsPendingByJobIdAndEmailAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AnyAsync(
                application => application.JobId == jobId
                    && application.ApplicantEmail == email
                    && application.Status == Domain.Enums.ApplicationStatus.PendingVerification,
                cancellationToken);
    }

    public async Task<JobApplication?> GetPendingByJobIdAndEmailAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .FirstOrDefaultAsync(
                application => application.JobId == jobId
                    && application.ApplicantEmail == email
                    && application.Status == Domain.Enums.ApplicationStatus.PendingVerification,
                cancellationToken);
    }

    public async Task<JobApplication?> GetAnyByJobIdAndEmailAsync(
        Guid jobId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .FirstOrDefaultAsync(
                application => application.JobId == jobId
                    && application.ApplicantEmail == email,
                cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.ApplicantEmail == email
                && application.Status == Domain.Enums.ApplicationStatus.PendingVerification)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplication>> GetByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.JobId == jobId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TalentX.Application.Common.Models.PaginatedList<JobApplication>> GetByJobIdPaginatedAsync(
        Guid jobId,
        string? search,
        TalentX.Domain.Enums.ApplicationStatus? status,
        string? sortBy,
        string? sortOrder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobApplications
            .AsNoTracking()
            .Where(application => application.JobId == jobId);

        if (status.HasValue)
        {
            query = query.Where(application => application.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(application =>
                application.ApplicantName.ToLower().Contains(searchLower) ||
                application.ApplicantEmail.ToLower().Contains(searchLower));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "appliedat" => sortOrder?.ToLowerInvariant() == "asc"
                ? query.OrderBy(application => application.AppliedAt)
                : query.OrderByDescending(application => application.AppliedAt),
            "status" => sortOrder?.ToLowerInvariant() == "asc"
                ? query.OrderBy(application => application.Status)
                : query.OrderByDescending(application => application.Status),
            _ => query.OrderByDescending(application => application.AiMatchScore ?? 0)
                .ThenByDescending(application => application.AppliedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new TalentX.Application.Common.Models.PaginatedList<JobApplication>(items, page, pageSize, totalCount);
    }

    public async Task<JobApplication?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .FirstOrDefaultAsync(application => application.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        JobApplication jobApplication,
        CancellationToken cancellationToken = default)
    {
        await _context.JobApplications.AddAsync(jobApplication, cancellationToken);
    }

    public async Task<PaginatedList<TalentApplicationRecord>> GetApplicationsForTalentAsync(
        string talentUserId,
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobApplications
                    .Join(
                        _context.Jobs,
                        application => application.JobId,
                        job => job.Id,
                        (application, job) => new
                        {
                            application,
                            job
                        })
                    .Where(x => x.application.TalentUserId == talentUserId)
                    .Select(x => new
                    {
                        x.application.Id,
                        x.application.JobId,
                        JobTitle = x.job.Title,
                        Company = x.job.CompanyName,
                        x.application.AppliedAt,
                        x.application.Status
                    });


        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        query = query.OrderByDescending(a => a.AppliedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var pagedItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var records = pagedItems.Select(item => new TalentApplicationRecord(
            item.Id,
            item.JobId,
            item.JobTitle,
            item.Company,
            item.AppliedAt,
            item.Status
        )).ToList();

        return new PaginatedList<TalentApplicationRecord>(records, page, pageSize, totalCount);
    }
    public async Task<int> CountHiredByEmployerIdAsync(
        string employerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .Where(application => application.Status == ApplicationStatus.Hired &&
                                  _context.Jobs.IgnoreQueryFilters().Any(job => job.Id == application.JobId && job.EmployerId == employerId))
            .CountAsync(cancellationToken);
    }

    public async Task<CandidatePipelineStatsDto> GetCandidatePipelineStatsByEmployerIdAsync(
        string employerId,
        CancellationToken cancellationToken = default)
    {
        var candidatePipelineStatistics = await _context.JobApplications
            .AsNoTracking()
            .Where(application => _context.Jobs.IgnoreQueryFilters().Any(job => job.Id == application.JobId && job.EmployerId == employerId))
            .GroupBy(application => 1)
            .Select(group => new
            {
                AppliedCount = group.Count(application => application.Status == ApplicationStatus.Applied),
                ShortlistedCount = group.Count(application => application.Status == ApplicationStatus.Shortlisted),
                InvitedCount = group.Count(application => application.Status == ApplicationStatus.Invited),
                InterviewCount = group.Count(application => application.Status == ApplicationStatus.Interview),
                RejectedCount = group.Count(application => application.Status == ApplicationStatus.Rejected),
                HiredCount = group.Count(application => application.Status == ApplicationStatus.Hired)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new CandidatePipelineStatsDto(
            candidatePipelineStatistics?.AppliedCount ?? 0,
            candidatePipelineStatistics?.ShortlistedCount ?? 0,
            candidatePipelineStatistics?.InvitedCount ?? 0,
            candidatePipelineStatistics?.InterviewCount ?? 0,
            candidatePipelineStatistics?.RejectedCount ?? 0,
            candidatePipelineStatistics?.HiredCount ?? 0);
    }

    public async Task<CandidatePipelineStatsDto> GetCandidatePipelineStatsByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var candidatePipelineStatistics = await _context.JobApplications
            .AsNoTracking()
            .Where(application => application.JobId == jobId)
            .GroupBy(application => 1)
            .Select(group => new
            {
                AppliedCount = group.Count(application => application.Status == ApplicationStatus.Applied),
                ShortlistedCount = group.Count(application => application.Status == ApplicationStatus.Shortlisted),
                InvitedCount = group.Count(application => application.Status == ApplicationStatus.Invited),
                InterviewCount = group.Count(application => application.Status == ApplicationStatus.Interview),
                RejectedCount = group.Count(application => application.Status == ApplicationStatus.Rejected),
                HiredCount = group.Count(application => application.Status == ApplicationStatus.Hired)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new CandidatePipelineStatsDto(
            candidatePipelineStatistics?.AppliedCount ?? 0,
            candidatePipelineStatistics?.ShortlistedCount ?? 0,
            candidatePipelineStatistics?.InvitedCount ?? 0,
            candidatePipelineStatistics?.InterviewCount ?? 0,
            candidatePipelineStatistics?.RejectedCount ?? 0,
            candidatePipelineStatistics?.HiredCount ?? 0);
    }

    public void Update(JobApplication jobApplication)
    {
        _context.JobApplications.Update(jobApplication);
    }

    public async Task DeleteByTalentUserIdAsync(string talentUserId, CancellationToken cancellationToken = default)
    {
        var applications = await _context.JobApplications
            .IgnoreQueryFilters()
            .Where(application => application.TalentUserId == talentUserId)
            .ToListAsync(cancellationToken);

        if (applications.Count > 0)
        {
            _context.JobApplications.RemoveRange(applications);
        }
    }

    public void Delete(JobApplication jobApplication)
    {
        _context.JobApplications.Remove(jobApplication);
    }

    public async Task<IReadOnlyDictionary<ApplicationStatus, int>> GetStatusCountsAppliedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobApplications
            .AsNoTracking()
            .Where(a => a.AppliedAt >= startUtc
                        && a.AppliedAt <= endUtc
                        && a.Status != ApplicationStatus.PendingVerification)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetAppliedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default)
    {
        var appliedAt = _context.JobApplications
            .AsNoTracking()
            .Where(a => a.AppliedAt >= startUtc
                        && a.AppliedAt <= endUtc
                        && a.Status != ApplicationStatus.PendingVerification)
            .Select(a => a.AppliedAt);

        return await TimeBucketCounter.CountAsync(appliedAt, groupByHour, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetHiredCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default)
    {
        var hiredAt = _context.JobApplications
            .AsNoTracking()
            .Where(a => a.Status == ApplicationStatus.Hired
                        && a.UpdatedAt != null
                        && a.UpdatedAt >= startUtc
                        && a.UpdatedAt <= endUtc)
            .Select(a => a.UpdatedAt!.Value);

        return await TimeBucketCounter.CountAsync(hiredAt, groupByHour, cancellationToken);
    }
}
