using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
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
}
