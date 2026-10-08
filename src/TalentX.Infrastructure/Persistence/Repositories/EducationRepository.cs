using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class EducationRepository : IEducationRepository
{
    private readonly ApplicationDbContext _context;

    public EducationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Education>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Educations
            .Where(education => education.UserId == userId)
            .OrderByDescending(education => education.StartDate)
            .ThenBy(education => education.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Education?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Educations
            .FirstOrDefaultAsync(education => education.Id == id, cancellationToken);
    }

    public async Task AddAsync(Education education, CancellationToken cancellationToken = default)
    {
        await _context.Educations.AddAsync(education, cancellationToken);
    }

    public void Update(Education education)
    {
        _context.Educations.Update(education);
    }

    public void Delete(Education education)
    {
        _context.Educations.Remove(education);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var educations = await _context.Educations
            .Where(education => education.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.Educations.RemoveRange(educations);
    }
}
