using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobApplicationStatusHistoryRepository : IJobApplicationStatusHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public JobApplicationStatusHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(JobApplicationStatusHistory history, CancellationToken cancellationToken = default)
    {
        await _context.JobApplicationStatusHistories.AddAsync(history, cancellationToken);
    }

    public async Task<IReadOnlyList<JobApplicationStatusHistory>> GetByApplicationIdAsync(Guid jobApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.JobApplicationStatusHistories
            .AsNoTracking()
            .Where(history => history.JobApplicationId == jobApplicationId)
            .OrderBy(history => history.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByApplicationIdsAsync(IEnumerable<Guid> jobApplicationIds, CancellationToken cancellationToken = default)
    {
        var jobApplicationIdList = jobApplicationIds.ToList();
        if (jobApplicationIdList.Count == 0)
        {
            return;
        }

        var histories = await _context.JobApplicationStatusHistories
            .Where(history => jobApplicationIdList.Contains(history.JobApplicationId))
            .ToListAsync(cancellationToken);

        if (histories.Count > 0)
        {
            _context.JobApplicationStatusHistories.RemoveRange(histories);
        }
    }
}
