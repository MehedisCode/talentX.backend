using Microsoft.EntityFrameworkCore;
using TalentX.Application.Common.Interfaces.Repositories;
using TalentX.Domain.Entities;
using TalentX.Domain.Enums;
using TalentX.Infrastructure.Persistence.Contexts;

namespace TalentX.Infrastructure.Persistence.Repositories;

public sealed class EmailDispatchLogRepository : IEmailDispatchLogRepository
{
    private readonly ApplicationDbContext _context;

    public EmailDispatchLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailDispatchLog log, CancellationToken cancellationToken = default)
    {
        await _context.EmailDispatchLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchLog>> ClaimPendingOrRetryableLogsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var candidateIds = await _context.EmailDispatchLogs
            .Where(x => (x.Status == EmailDispatchStatus.Pending || x.Status == EmailDispatchStatus.Failed)
                        && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt)
            .Take(batchSize)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            return Array.Empty<EmailDispatchLog>();
        }

        // Re-checks Status in the WHERE clause so this only claims rows still Pending/Failed at
        // the moment of the update. If a concurrent caller (e.g. another worker instance) already
        // claimed one of these ids, this update simply won't affect that row — no exception, no
        // lock, it's just excluded from the result below. This bypasses SaveChangesAsync and
        // commits immediately, unlike the rest of this app's unit-of-work pattern, which is
        // intentional: the claim must be visible to other instances right away.
        await _context.EmailDispatchLogs
            .Where(x => candidateIds.Contains(x.Id)
                        && (x.Status == EmailDispatchStatus.Pending || x.Status == EmailDispatchStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, EmailDispatchStatus.Processing)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.LastAttemptAt, now), cancellationToken);

        return await _context.EmailDispatchLogs
            .Where(x => candidateIds.Contains(x.Id) && x.Status == EmailDispatchStatus.Processing)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchLog>> GetStaleProcessingLogsAsync(
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        var staleBefore = DateTime.UtcNow.Subtract(staleAfter);

        return await _context.EmailDispatchLogs
            .Where(x => x.Status == EmailDispatchStatus.Processing && x.LastAttemptAt <= staleBefore)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailDispatchLog?> GetLatestByEmailAsync(
        string recipientEmail,
        CancellationToken cancellationToken = default)
    {
        return await _context.EmailDispatchLogs
            .Where(x => x.RecipientEmail.ToLower() == recipientEmail.ToLower())
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Update(EmailDispatchLog log)
    {
        _context.EmailDispatchLogs.Update(log);
    }
}
