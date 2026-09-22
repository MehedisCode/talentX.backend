using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUserIdAsync(string userId)
    {
        return await _context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        UserQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _context.DomainUsers.AsNoTracking().Where(u => u.Role != UserRole.Admin);

        if (!string.IsNullOrWhiteSpace(queryParams.SearchQuery))
        {
            var normalizedSearchQuery = queryParams.SearchQuery.Trim().ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(normalizedSearchQuery) || u.Email.ToLower().Contains(normalizedSearchQuery));
        }

        if (queryParams.Role.HasValue)
        {
            query = query.Where(u => u.Role == queryParams.Role.Value);
        }

        if (queryParams.Status.HasValue)
        {
            query = query.Where(u => u.Status == queryParams.Status.Value);
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyDictionary<UserRole, int> RoleCounts, IReadOnlyDictionary<AccountStatus, int> StatusCounts)> GetRoleAndStatusCountsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _context.DomainUsers.AsNoTracking().Where(u => u.Role != UserRole.Admin);

        var roleCounts = await query
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count, cancellationToken);

        var statusCounts = await query
            .GroupBy(u => u.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return (roleCounts, statusCounts);
    }

    public async Task<IReadOnlyList<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.DomainUsers
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Admin)
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.DomainUsers.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.DomainUsers.Update(user);
    }
}

