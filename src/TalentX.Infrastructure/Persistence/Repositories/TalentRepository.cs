using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class TalentRepository : ITalentRepository
{
    private readonly ApplicationDbContext _context;

    public TalentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Talent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .FirstOrDefaultAsync(talent => talent.Id == id, cancellationToken);
    }

    public async Task<Talent?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .FirstOrDefaultAsync(talent => talent.UserId == userId, cancellationToken);
    }

    public async Task<Talent?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .FirstOrDefaultAsync(talent => talent.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Talent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .AsNoTracking()
            .OrderBy(talent => talent.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .AsNoTracking()
            .CountAsync(talent => talent.CreatedAt >= startUtc && talent.CreatedAt <= endUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<Talent>> GetRecentCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.Talents
            .AsNoTracking()
            .Where(talent => talent.CreatedAt >= startUtc && talent.CreatedAt <= endUtc)
            .OrderByDescending(talent => talent.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Talent talent, CancellationToken cancellationToken = default)
    {
        await _context.Talents.AddAsync(talent, cancellationToken);
    }

    public void Update(Talent talent)
    {
        _context.Talents.Update(talent);
    }
}
