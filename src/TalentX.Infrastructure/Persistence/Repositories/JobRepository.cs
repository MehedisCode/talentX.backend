using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Application.Features.Employers.Constants;
using TalentX.Application.Features.Employers.DTOs;
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
                j => j.Id == id && j.Status == JobStatus.Published && j.ExpireAt.Date >= DateTime.UtcNow.Date,
                cancellationToken);

    public async Task<List<Job>> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .AsNoTracking()
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
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
                Expired = group.Count(job => job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new EmployerJobStatsDto(
            statistics?.TotalPosted ?? 0,
            statistics?.Active ?? 0,
            statistics?.Draft ?? 0,
            statistics?.Closed ?? 0,
            statistics?.Expired ?? 0);
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
            job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate
                ? "Expired"
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
            else
            {
                query = query.Where(job => job.Status == status.Value);
            }
        }
        else if (isExpired == true)
        {
            query = query.Where(job => job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate);
        }
        else if (isExpired == false)
        {
            query = query.Where(job => !(job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate));
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
                ApplicantCount = Context.JobApplications.Count(application => application.JobId == job.Id)
            })
            .ToListAsync(cancellationToken);

        var items = pagedJobs.Select(job => new EmployerJobListItemDto(
            job.Id,
            job.Title,
            job.CompanyName,
            job.Status == JobStatus.Published && job.ExpireAt.Date < currentDate
                ? "Expired"
                : job.Status.ToString(),
            job.EmploymentType,
            job.WorkArrangement,
            job.ExperienceLevel,
            job.Location,
            job.Vacancies,
            job.ExpireAt,
            job.CreatedAt,
            job.ApplicantCount)).ToList();

        return new PaginatedList<EmployerJobListItemDto>(items, page, pageSize, totalCount);
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
}
