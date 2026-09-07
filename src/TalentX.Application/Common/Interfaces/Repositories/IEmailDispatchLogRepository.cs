using TalentX.Domain.Entities;

namespace TalentX.Application.Common.Interfaces.Repositories;

public interface IEmailDispatchLogRepository
{
    Task AddAsync(EmailDispatchLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> pending/retryable logs by marking
    /// them Processing as part of the same query, so two concurrent callers (e.g. two worker
    /// instances) never both receive the same row.
    /// </summary>
    Task<IReadOnlyList<EmailDispatchLog>> ClaimPendingOrRetryableLogsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<EmailDispatchLog?> GetLatestByEmailAsync(string recipientEmail, CancellationToken cancellationToken = default);
    void Update(EmailDispatchLog log);
}
