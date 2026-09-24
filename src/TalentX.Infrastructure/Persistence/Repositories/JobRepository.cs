using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
using TalentX.Application.Features.Jobs.DTOs;
using TalentX.Application.Features.Jobs.Moderation;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobRepository : RepositoryBase<Job>, IJobRepository
{
    public JobRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<PaginatedList<Job>> GetPublishedJobsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken,
        string? keyword = null,
        string? location = null,
        EmploymentType? employmentType = null,
        ExperienceLevel? experienceLevel = null,
        WorkArrangement? workArrangement = null)
    {
        var query = Context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Published && j.ExpireAt.Date >= DateTime.UtcNow.Date);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(kw) ||
                j.CompanyName.ToLower().Contains(kw) ||
                j.Description.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var loc = location.Trim().ToLower();
            query = query.Where(j =>
                j.Location.ToLower().Contains(loc) ||
                (j.City != null && j.City.ToLower().Contains(loc)) ||
                (j.Country != null && j.Country.ToLower().Contains(loc)));
        }

        if (employmentType.HasValue)
            query = query.Where(j => j.EmploymentType == employmentType.Value);

        if (experienceLevel.HasValue)
            query = query.Where(j => j.ExperienceLevel == experienceLevel.Value);

        if (workArrangement.HasValue)
            query = query.Where(j => j.WorkArrangement == workArrangement.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
            .ThenByDescending(j => j.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Job>(items, page, pageSize, totalCount);
    }

    public async Task<Job?> GetPublishedJobByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await Context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == id && j.Status == JobStatus.Published,
                cancellationToken);

    public async Task<List<Job>> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .AsNoTracking()
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<Job>> GetByEmployerIdIncludingDeletedAsync(string employerId, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .IgnoreQueryFilters()
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<Job>> GetActivePublishedJobsByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .AsNoTracking()
            .Where(j => j.EmployerId == employerId && 
            j.Status == JobStatus.Published && 
            j.IsDeleted == false &&
            j.ExpireAt.Date >= DateTime.UtcNow.Date)
            .OrderByDescending(j => j.PublishedAt ?? j.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Job>> GetActivePublishedJobsAsync(CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .Where(j => j.ExpireAt.Date >= DateTime.UtcNow.Date && j.Status == JobStatus.Published)
            .ToListAsync(cancellationToken);
    public async Task<EmployerJobStatsDto> GetEmployerStatsAsync(
        string employerId,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow.Date;

        var statistics = await Context.Jobs
            .AsNoTracking()
            .Where(job => job.EmployerId == employerId)
            .GroupBy(job => 1)
            .Select(group => new
            {
                TotalPosted = group.Count(),
                Active = group.Count(job => job.Status == JobStatus.Published && job.ExpireAt.Date >= currentDate),
                Draft = group.Count(job => job.Status == JobStatus.Draft),
                Closed = group.Count(job => job.Status == JobStatus.Closed),
                Expired = group.Count(job => job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate)),
                // Without these, moderated jobs still count in TotalPosted but appear in no breakdown.
                Deactivated = group.Count(job => job.Status == JobStatus.Deactivated),
                PendingReview = group.Count(job => job.Status == JobStatus.PendingReview)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new EmployerJobStatsDto(
            statistics?.TotalPosted ?? 0,
            statistics?.Active ?? 0,
            statistics?.Draft ?? 0,
            statistics?.Closed ?? 0,
            statistics?.Expired ?? 0,
            statistics?.Deactivated ?? 0,
            statistics?.PendingReview ?? 0);
    }

    public async Task<List<EmployerRecentJobDto>> GetRecentJobsByEmployerIdAsync(
        string employerId,
        int count = EmployerDashboardConstants.RecentJobsLimit,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow.Date;

        var recentJobs = await Context.Jobs
            .AsNoTracking()
            .Where(job => job.EmployerId == employerId)
            .OrderByDescending(job => job.CreatedAt)
            .Take(count)
            .Select(job => new
            {
                job.Id,
                job.Title,
                job.Status,
                job.CreatedAt,
                job.ExpireAt,
                job.Vacancies,
                ApplicantCount = Context.JobApplications.Count(application => application.JobId == job.Id)
            })
            .ToListAsync(cancellationToken);

        return recentJobs.Select(job => new EmployerRecentJobDto(
            job.Id,
            job.Title,
            job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate)
                ? JobStatus.Expired.ToString()
                : job.Status.ToString(),
            job.CreatedAt,
            job.ExpireAt,
            job.Vacancies,
            job.ApplicantCount)).ToList();
    }

    public async Task<PaginatedList<EmployerJobListItemDto>> GetEmployerJobsPaginatedAsync(
        string employerId,
        JobStatus? status,
        bool? isExpired,
        int page,
        int pageSize,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow.Date;
        var query = Context.Jobs
            .AsNoTracking()
            .Where(job => job.EmployerId == employerId);

        if (status.HasValue)
        {
            if (status.Value == JobStatus.Published && isExpired == true)
            {
                query = query.Where(job => job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate);
            }
            else if (status.Value == JobStatus.Published && isExpired == false)
            {
                query = query.Where(job => job.Status == JobStatus.Published && job.ExpireAt.Date >= currentDate);
            }
            else if (status.Value == JobStatus.Expired)
            {
                query = query.Where(job => job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate));
            }
            else
            {
                query = query.Where(job => job.Status == status.Value);
            }
        }
        else if (isExpired == true)
        {
            query = query.Where(job => job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate));
        }
        else if (isExpired == false)
        {
            query = query.Where(job => !(job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate)));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var trimmedSearchTerm = searchTerm.Trim().ToLower();
            query = query.Where(job =>
                job.Title.ToLower().Contains(trimmedSearchTerm) ||
                job.CompanyName.ToLower().Contains(trimmedSearchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedJobs = await query
            .OrderByDescending(job => job.CreatedAt)
            .ThenByDescending(job => job.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(job => new
            {
                job.Id,
                job.Title,
                job.CompanyName,
                job.Status,
                job.EmploymentType,
                job.WorkArrangement,
                job.ExperienceLevel,
                job.Location,
                job.Vacancies,
                job.ExpireAt,
                job.CreatedAt,
                job.SalaryType,
                job.SalaryMin,
                job.SalaryMax,
                job.SalaryCurrency,
                job.SalaryPeriod,
                job.DeactivationReason,
                ApplicantCount = Context.JobApplications.Count(application => application.JobId == job.Id)
            })
            .ToListAsync(cancellationToken);

        var items = pagedJobs.Select(job => new EmployerJobListItemDto(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Status == JobStatus.Expired || (job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate)
                ? JobStatus.Expired.ToString()
                : job.Status.ToString(),
            job.EmploymentType,
            job.WorkArrangement,
            job.ExperienceLevel,
            job.Location,
            job.Vacancies,
            job.ExpireAt,
            job.CreatedAt,
            job.ApplicantCount,
            job.SalaryType,
            job.SalaryMin,
            job.SalaryMax,
            job.SalaryCurrency,
            job.SalaryPeriod,
            job.DeactivationReason.HasValue
                ? DeactivationReasonLabels.ToLabel(job.DeactivationReason.Value)
                : null)).ToList();

        return new PaginatedList<EmployerJobListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<PaginatedList<AdminJobListItemDto>> GetAdminJobsPagedAsync(
        AdminJobQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        var currentDate = DateTime.UtcNow.Date;
        var moderationStatuses = ModerationStatuses.All;

        // Left join to users: employer name and email are what "search by employer" matches
        // on, and a job must still be listed even if its user record is missing.
        // Soft-deleted jobs are already excluded by Job's global query filter.
        var query =
            from job in Context.Jobs.AsNoTracking()
            join user in Context.DomainUsers.AsNoTracking() on job.EmployerId equals user.UserId into employerMatches
            from employer in employerMatches.DefaultIfEmpty()
            where moderationStatuses.Contains(job.Status)
            select new { Job = job, Employer = employer };

        if (queryParams.Status.HasValue)
        {
            query = query.Where(row => row.Job.Status == queryParams.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.SearchQuery))
        {
            var normalizedSearchQuery = queryParams.SearchQuery.Trim().ToLower();
            query = query.Where(row =>
                row.Job.Title.ToLower().Contains(normalizedSearchQuery) ||
                row.Job.CompanyName.ToLower().Contains(normalizedSearchQuery) ||
                row.Job.Location.ToLower().Contains(normalizedSearchQuery) ||
                (row.Employer != null && row.Employer.Name.ToLower().Contains(normalizedSearchQuery)) ||
                (row.Employer != null && row.Employer.Email.ToLower().Contains(normalizedSearchQuery)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedRows = await query
            // Jobs waiting on the admin are the work queue, so they come first.
            .OrderBy(row => row.Job.Status == JobStatus.PendingReview ? 0 : 1)
            .ThenByDescending(row => row.Job.UpdatedAt ?? row.Job.CreatedAt)
            .ThenByDescending(row => row.Job.Id)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(row => new
            {
                row.Job.Id,
                row.Job.Title,
                row.Job.CompanyName,
                EmployerName = row.Employer != null ? row.Employer.Name : string.Empty,
                EmployerEmail = row.Employer != null ? row.Employer.Email : string.Empty,
                row.Job.Status,
                row.Job.Location,
                row.Job.EmploymentType,
                row.Job.WorkArrangement,
                row.Job.PublishedAt,
                row.Job.ExpireAt,
                row.Job.DeactivatedAt,
                ApplicantCount = Context.JobApplications.Count(application => application.JobId == row.Job.Id)
            })
            .ToListAsync(cancellationToken);

        var items = pagedRows.Select(row => new AdminJobListItemDto
        {
            Id = row.Id,
            Title = row.Title,
            CompanyName = row.CompanyName,
            EmployerName = row.EmployerName,
            EmployerEmail = row.EmployerEmail,
            Status = row.Status,
            IsExpired = row.Status == JobStatus.Published && row.ExpireAt.Date < currentDate,
            Location = row.Location,
            EmploymentType = row.EmploymentType,
            WorkArrangement = row.WorkArrangement,
            ApplicantCount = row.ApplicantCount,
            PublishedAt = row.PublishedAt,
            ExpireAt = row.ExpireAt,
            DeactivatedAt = row.DeactivatedAt
        }).ToList();

        return new PaginatedList<AdminJobListItemDto>(items, queryParams.Page, queryParams.PageSize, totalCount);
    }

    public async Task<Job?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<List<Job>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => ids.Contains(j.Id))
            .ToListAsync(cancellationToken);

    public void RemoveRange(IEnumerable<Job> jobs)
    {
        Context.Jobs.RemoveRange(jobs);
    }

    public async Task<IReadOnlyDictionary<JobStatus, int>> GetStatusCountsCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await Context.Jobs
            .AsNoTracking()
            .Where(j => j.CreatedAt >= startUtc && j.CreatedAt <= endUtc && j.Status != JobStatus.Draft)
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<DateTime, int>> GetPublishedCountsByBucketAsync(
        DateTime startUtc,
        DateTime endUtc,
        bool groupByHour,
        CancellationToken cancellationToken = default)
    {
        var publishedAt = Context.Jobs
            .AsNoTracking()
            .Where(j => j.PublishedAt != null && j.PublishedAt >= startUtc && j.PublishedAt <= endUtc)
            .Select(j => j.PublishedAt!.Value);

        return await TimeBucketCounter.CountAsync(publishedAt, groupByHour, cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> GetRecentPublishedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await Context.Jobs
            .AsNoTracking()
            .Where(j => j.PublishedAt != null && j.PublishedAt >= startUtc && j.PublishedAt <= endUtc)
            .OrderByDescending(j => j.PublishedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
