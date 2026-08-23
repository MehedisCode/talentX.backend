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

    public async Task<PaginatedList<Job>> GetPublishedJobsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = Context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Published && j.ExpireAt > DateTime.UtcNow);

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
}
