using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobAlertRepository : IJobAlertRepository
{
    private readonly ApplicationDbContext _context;

    public JobAlertRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<JobAlert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.JobAlerts
            .FirstOrDefaultAsync(alert => alert.Id == id, cancellationToken);
    }

    public async Task<List<JobAlert>> GetByTalentIdAsync(string talentId, CancellationToken cancellationToken = default)
    {
        return await _context.JobAlerts
            .Where(alert => alert.TalentId == talentId)
            .OrderByDescending(alert => alert.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByTalentIdAsync(string talentId, CancellationToken cancellationToken = default)
    {
        return await _context.JobAlerts
            .CountAsync(alert => alert.TalentId == talentId, cancellationToken);
    }

    public async Task AddAsync(JobAlert entity, CancellationToken cancellationToken = default)
    {
        await _context.JobAlerts.AddAsync(entity, cancellationToken);
    }

    public void Remove(JobAlert entity)
    {
        _context.JobAlerts.Remove(entity);
    }

    public async Task<List<JobAlert>> GetActiveAlertsByTalentIdAsync(string talentId, CancellationToken cancellationToken = default)
    {
        return await _context.JobAlerts
            .Where(alert => alert.TalentId == talentId && alert.IsEnabled)
            .OrderByDescending(alert => alert.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
