using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class EmployerRepository : IEmployerRepository
{
    private readonly ApplicationDbContext _context;

    public EmployerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Employer employer, CancellationToken cancellationToken = default)
    {
        await _context.Employers.AddAsync(employer, cancellationToken);
    }

    public void Update(Employer employer)
    {
        _context.Employers.Update(employer);
    }

    public async Task<Employer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
    }

    public async Task<Employer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .AnyAsync(e => e.Email == email, cancellationToken);
    }

    public async Task<(IReadOnlyList<Employer> Items, int TotalCount)> GetPagedAsync(
        string? search,
        EmployerVerificationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Employers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(e =>
                e.Name.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.CompanyName.ToLower().Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.VerificationStatus == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyDictionary<EmployerVerificationStatus, int>> GetVerificationStatusCountsCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .AsNoTracking()
            .Where(e => e.CreatedAt >= startUtc && e.CreatedAt <= endUtc)
            .GroupBy(e => e.VerificationStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
    }

    public async Task<DateTime?> GetOldestPendingCreatedAtAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .AsNoTracking()
            .Where(e => e.VerificationStatus == EmployerVerificationStatus.PendingVerification)
            .OrderBy(e => e.CreatedAt)
            .Select(e => (DateTime?)e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employer>> GetRecentCreatedBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _context.Employers
            .AsNoTracking()
            .Where(e => e.CreatedAt >= startUtc && e.CreatedAt <= endUtc)
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
