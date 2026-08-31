using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
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
            .Where(j => j.Status == JobStatus.Published && j.ExpireAt > DateTime.UtcNow);

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
            .OrderByDescending(j => j.CreatedAt)
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
                j => j.Id == id && j.Status == JobStatus.Published && j.ExpireAt > DateTime.UtcNow,
                cancellationToken);

    public async Task<List<Job>> GetByEmployerIdAsync(string employerId, CancellationToken cancellationToken = default) =>
        await Context.Jobs
            .AsNoTracking()
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
}
