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

    public async Task<IReadOnlyList<EmailDispatchLog>> GetPendingOrRetryableLogsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.EmailDispatchLogs
            .Where(x => (x.Status == EmailDispatchStatus.Pending || x.Status == EmailDispatchStatus.Failed)
                        && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt)
            .Take(batchSize)
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
