using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Application.Common.Models;
using TalentX.Domain.Entities;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<AuditLog> auditLogs, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _context.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAuditLogsAsync(
        AuditLogQueryParams queryParams,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParams.SearchQuery))
        {
            var normalizedSearchQuery = queryParams.SearchQuery.Trim().ToLower();
            query = query.Where(auditLog => auditLog.Actor.ToLower().Contains(normalizedSearchQuery) || auditLog.Action.ToLower().Contains(normalizedSearchQuery));
        }

        if (queryParams.StartDate.HasValue)
        {
            var utcStart = queryParams.StartDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(queryParams.StartDate.Value, DateTimeKind.Utc)
                : queryParams.StartDate.Value.ToUniversalTime();
            query = query.Where(x => x.CreatedAt >= utcStart);
        }

        if (queryParams.EndDate.HasValue)
        {
            var utcEnd = queryParams.EndDate.Value.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(queryParams.EndDate.Value, DateTimeKind.Utc)
                : queryParams.EndDate.Value.ToUniversalTime();
            query = query.Where(x => x.CreatedAt <= utcEnd);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
