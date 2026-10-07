using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class WorkExperienceRepository : IWorkExperienceRepository
{
    private readonly ApplicationDbContext _context;

    public WorkExperienceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkExperience>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.WorkExperiences
            .Where(workExperience => workExperience.UserId == userId)
            .OrderByDescending(workExperience => workExperience.StartDate)
            .ThenBy(workExperience => workExperience.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkExperience?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WorkExperiences
            .FirstOrDefaultAsync(workExperience => workExperience.Id == id, cancellationToken);
    }

    public async Task AddAsync(WorkExperience workExperience, CancellationToken cancellationToken = default)
    {
        await _context.WorkExperiences.AddAsync(workExperience, cancellationToken);
    }

    public void Update(WorkExperience workExperience)
    {
        _context.WorkExperiences.Update(workExperience);
    }

    public void Delete(WorkExperience workExperience)
    {
        _context.WorkExperiences.Remove(workExperience);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var experiences = await _context.WorkExperiences
            .Where(workExperience => workExperience.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.WorkExperiences.RemoveRange(experiences);
    }
}
