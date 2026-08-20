using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public class JobRepository : IJobRepository
{
    private readonly ApplicationDbContext _context;

    public JobRepository(ApplicationDbContext context) => _context = context;

    public async Task<(IReadOnlyList<Job> Items, int TotalCount)> GetPublishedJobsAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Jobs
            .AsNoTracking()
            .Where(j => j.Status == JobStatus.Published
                     && j.ExpireAt > DateTime.UtcNow);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .ThenByDescending(j => j.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Job?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == id
                  && j.Status == JobStatus.Published
                  && j.ExpireAt > DateTime.UtcNow,
                cancellationToken);

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Job>().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await _context.Set<Job>().AddAsync(job, cancellationToken);
    }

    public void Update(Job job)
    {
        _context.Set<Job>().Update(job);
    }
}
