using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobAlertNotificationHistoryRepository : IJobAlertNotificationHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public JobAlertNotificationHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string talentId, Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobAlertNotificationHistories
            .AnyAsync(history => history.TalentId == talentId && history.JobId == jobId, cancellationToken);
    }

    public async Task<HashSet<string>> GetNotifiedTalentIdsAsync(Guid jobId, IEnumerable<string> talentIds, CancellationToken cancellationToken = default)
    {
        var talentIdList = talentIds.ToList();
        if (talentIdList.Count == 0)
        {
            return [];
        }

        var notifiedIds = await _context.JobAlertNotificationHistories
            .Where(history => history.JobId == jobId && talentIdList.Contains(history.TalentId))
            .Select(history => history.TalentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return notifiedIds.ToHashSet();
    }

    public async Task AddAsync(JobAlertNotificationHistory history, CancellationToken cancellationToken = default)
    {
        await _context.JobAlertNotificationHistories.AddAsync(history, cancellationToken);
    }
}
