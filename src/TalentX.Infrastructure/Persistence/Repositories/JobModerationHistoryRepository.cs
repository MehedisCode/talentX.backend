using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobModerationHistoryRepository : IJobModerationHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public JobModerationHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JobModerationHistory history, CancellationToken cancellationToken = default)
    {
        await _context.JobModerationHistories.AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyList<JobModerationHistory>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobModerationHistories
            .AsNoTracking()
            .Where(history => history.JobId == jobId)
            .OrderByDescending(history => history.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
