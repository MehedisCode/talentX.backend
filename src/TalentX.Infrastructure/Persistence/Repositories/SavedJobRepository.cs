using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class SavedJobRepository : ISavedJobRepository
{
    private readonly ApplicationDbContext _context;

    public SavedJobRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SavedJob?> FindAsync(
        string talentId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavedJobs
            .FirstOrDefaultAsync(
                savedJob => savedJob.TalentId == talentId && savedJob.JobId == jobId,
                cancellationToken);
    }

    public async Task AddAsync(
        SavedJob entity,
        CancellationToken cancellationToken = default)
    {
        await _context.SavedJobs.AddAsync(entity, cancellationToken);
    }

    public void Remove(SavedJob entity)
    {
        _context.SavedJobs.Remove(entity);
    }

    public async Task<PaginatedList<SavedJob>> GetSavedJobsAsync(
        string talentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SavedJobs
            .Where(savedJob => savedJob.TalentId == talentId)
            .OrderByDescending(savedJob => savedJob.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<SavedJob>(items, page, pageSize, totalCount);
    }

    public async Task<List<Guid>> GetSavedJobIdsAsync(
        string talentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.SavedJobs
            .Where(savedJob => savedJob.TalentId == talentId)
            .Select(savedJob => savedJob.JobId)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByTalentIdAsync(string talentId, CancellationToken cancellationToken = default)
    {
        var savedJobs = await _context.SavedJobs
            .Where(savedJob => savedJob.TalentId == talentId)
            .ToListAsync(cancellationToken);

        if (savedJobs.Count > 0)
        {
            _context.SavedJobs.RemoveRange(savedJobs);
        }
    }
}
