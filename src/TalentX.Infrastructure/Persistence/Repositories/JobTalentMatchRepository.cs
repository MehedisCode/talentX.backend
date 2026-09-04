using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class JobTalentMatchRepository : IJobTalentMatchRepository
{
    private readonly ApplicationDbContext _context;

    public JobTalentMatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<JobTalentMatch>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        return await _context.JobTalentMatches
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.Score)
            .ToListAsync(cancellationToken);
    }

    public async Task<JobTalentMatch?> GetByJobIdAndTalentUserIdAsync(
        Guid jobId,
        string talentUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.JobTalentMatches
            .FirstOrDefaultAsync(
                x => x.JobId == jobId && x.TalentUserId == talentUserId,
                cancellationToken);
    }

    public async Task AddAsync(JobTalentMatch match, CancellationToken cancellationToken = default)
    {
        await _context.JobTalentMatches.AddAsync(match, cancellationToken);
    }

    public void Update(JobTalentMatch match)
    {
        _context.JobTalentMatches.Update(match);
    }

    public async Task DeleteByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var existingMatches = await _context.JobTalentMatches
            .Where(x => x.JobId == jobId)
            .ToListAsync(cancellationToken);

        if (existingMatches.Count > 0)
        {
            _context.JobTalentMatches.RemoveRange(existingMatches);
        }
    }
}
